namespace Microsoft.InnerEye.Gateway.MessageQueueing.Sqlite
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Sqlite;
    using Microsoft.InnerEye.Gateway.Sqlite.Extensions;
    using Newtonsoft.Json;

    /// <summary>
    /// Message queue service using SQLite as the backend database.
    /// </summary>
    public class SqliteMessageQueue : IMessageQueue
    {
        /// <summary>
        /// The database connection path.
        /// Note: To explore the database file you can use the tool: https://sqlitebrowser.org/
        /// </summary>
        private const string DatabaseConnectionStringFormat = @"Data Source={0}\MicrosoftInnerEyeGatewayMessageQueue.db;";

        /// <summary>
        /// The data column (name & data type).
        /// </summary>
        private static readonly (string ColumnName, string ColumnDataType) QueueTableDataColumn = ("data", "TEXT");

        /// <summary>
        /// The transaction identifier column (name & data type).
        /// </summary>
        private static readonly (string ColumnName, string ColumnDataType) QueueTableTransactionIdColumn = ("transactionId", "VARCHAR(36)");

        /// <summary>
        /// The transaction lease column (name & data type).
        /// </summary>
        private static readonly (string ColumnName, string ColumnDataType) QueueTableTransactionLeaseColumn = ("transactionLease", "INT");

        /// <summary>
        /// The row unique identifier column (name & data type).
        /// </summary>
        private static readonly (string ColumnName, string ColumnDataType) QueueTableRowIdColumn = ("rowId", "VARCHAR(36)");

        /// <summary>
        /// The enqueue row identifier column (name & data type).
        /// </summary>
        private static readonly (string ColumnName, string ColumnDataType) QueueTableEnqueueRowIdColumn = ("enqueueRowId", "VARCHAR(36)");

        /// <summary>
        /// The row enqueue time column (name & data type).
        /// </summary>
        private static readonly (string ColumnName, string ColumnDataType) QueueTableEnqueueTimeColumn = ("enqueueTime", "INT");

        /// <summary>
        /// The queue table data column names and data type.
        /// </summary>
        private static readonly (string ColumnName, string ColumnDataType)[] QueueTableDataColumnNames = new (string, string)[]
        {
            QueueTableDataColumn,
            QueueTableTransactionIdColumn,
            QueueTableTransactionLeaseColumn,
            QueueTableRowIdColumn,
            QueueTableEnqueueRowIdColumn,
            QueueTableEnqueueTimeColumn
        };

        /// <summary>
        /// SqliteManager object if Logger config is set to SQLite
        /// </summary>
        public SqliteManager SqliteManager { get; set; }
        /// <summary>
        /// The lease of a transaction in milliseconds.
        /// </summary>
        private readonly uint _transactionLeaseMs;

        /// <summary>
        /// The delay in milliseconds the renew lease task will attempt to renew the lease of a row.
        /// </summary>
        private readonly uint _transactionRenewLeaseMs;

        /// <summary>
        /// The connection string to the database.
        /// </summary>
        private readonly string _databaseConnectionString;

        /// <summary>
        /// The table name.
        /// </summary>
        private readonly string _tableName;

        /// <summary>
        /// If this instance is disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteMessageQueue"/> class.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="transactionLeaseMs">
        /// The lease of a transaction in milliseconds (Default is 1 minute, which will be renewed every 30 seconds).
        /// The value must be larger than 1 second to allow the renew task time to renew a rows lease.
        /// </param>
        /// <exception cref="ArgumentException">If the table name is null or whitespace or the transaction lease is less than 1 second.</exception>
        /// <exception cref="SqliteException">If we fail to open a connection to the database.</exception>
        public SqliteMessageQueue(string tableName, uint transactionLeaseMs = 60 * 1000)
        {
            _tableName = !string.IsNullOrWhiteSpace(tableName) ? tableName : throw new ArgumentException("tableName should be non-empty", nameof(tableName));

            _transactionLeaseMs = transactionLeaseMs >= 1000 ? transactionLeaseMs : throw new ArgumentException("transactionLeaseMs should be at least 1000", nameof(transactionLeaseMs));
            _transactionRenewLeaseMs = transactionLeaseMs / 2; // Attempt to renew leases using timeout divided by 2

            SqliteManager = new SqliteManager(tableName, DatabaseConnectionStringFormat, QueueTableDataColumnNames);
            _databaseConnectionString = SqliteManager.DatabaseConnectionString;
        }

        /// <summary>
        /// The name of the table the SQLite message queue will write queue messages into.
        /// </summary>
        public string QueuePath => _tableName;

        /// <summary>
        /// Gets the database connection string.
        /// </summary>
        public string DatabaseConnectionString => _databaseConnectionString;

        /// <inheritdoc />
        public IQueueTransaction CreateQueueTransaction()
        {
            return new SqliteMessageQueueTransaction(_databaseConnectionString, _transactionRenewLeaseMs);
        }

        /// <inheritdoc />
        public void Clear()
        {
            SqliteExtensions.ExecuteNonQueryNewConnection(
                connectionString: _databaseConnectionString,
                commandText: string.Format(CultureInfo.InvariantCulture, SqliteManager.ClearTableCommandTextFormat, QueuePath));
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">If the queue transaction is null or is not the correct type.</exception>
        /// <exception cref="ArgumentNullException">If input value is null.</exception>
        /// <exception cref="MessageQueueWriteException">If the queue cannot write the input value.</exception>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void Enqueue<T>(T value, IQueueTransaction queueTransaction)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!(queueTransaction is SqliteMessageQueueTransaction transaction))
            {
                throw new ArgumentException("queueTransaction is not a SqliteMessageQueueTransaction", nameof(queueTransaction));
            }

            // Create a unique row ID for this item (this will also be used for the commit and abort commands)
            var rowId = Guid.NewGuid();

            using (var command = transaction.GetSqliteCommand())
            {
                // Note: Make sure you call ToString() here on the row identifier
                command.Parameters.AddWithValue($"@{QueueTableRowIdColumn.ColumnName}", rowId.ToString());
                // Create enqueue row identifier, that should never be touched
                command.Parameters.AddWithValue($"@{QueueTableEnqueueRowIdColumn.ColumnName}", rowId.ToString());
                command.Parameters.AddWithValue($"@{QueueTableDataColumn.ColumnName}", JsonConvert.SerializeObject(value));
                command.Parameters.AddWithValue($"@{QueueTableTransactionIdColumn.ColumnName}", transaction.TransactionId);
                command.Parameters.AddWithValue($"@{QueueTableTransactionLeaseColumn.ColumnName}", 0);
                command.Parameters.AddWithValue($"@{QueueTableEnqueueTimeColumn.ColumnName}", DateTime.UtcNow.Ticks);

                // Execute the query and insert the row
                var result = command.ExecuteNonQueryWithRetry(string.Format(CultureInfo.InvariantCulture, SqliteManager.GetInsertRowCommandFormat(), QueuePath));

                if (result == 0)
                {
                    throw new MessageQueueWriteException("Failed to write to the SQLite message queue table.");
                }

                // Create a new command for commit to remove the lock on this row (note: we read by enqueue row identifier)
                transaction.EnqueueCommitCommand(
                    GetUpdateSqlCommandText(
                        new[] { (QueueTableTransactionIdColumn.ColumnName, string.Empty) }, // On commit, remove the transaction lock
                        GetWhereClauseRowIdCommandText(rowId, QueueTableEnqueueRowIdColumn.ColumnName)));

                // Create a new command when the transaction is aborted to delete the row (note: we read by enqueue row identifier).
                transaction.EnqueueAbortCommand(GetDeleteRowSqlCommandText(rowId, QueueTableEnqueueRowIdColumn.ColumnName));
            }
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException">If the queue transaction is null or is not the correct type.</exception>
        /// <exception cref="MessageQueueReadException">If the queue deos not have any items on the queue.</exception>
        public T DequeueNextMessage<T>(IQueueTransaction queueTransaction)
        {
            if (!(queueTransaction is SqliteMessageQueueTransaction transaction))
            {
                throw new ArgumentException("queueTransaction is not a SqliteMessageQueueTransaction", nameof(queueTransaction));
            }

            var result = default(T);
            var rowId = Guid.NewGuid();

            using (var command = transaction.GetSqliteCommand())
            {
                // Attempt to update one row by setting it as not readable by other readers.
                // We set a lease on this item, and clear out any transaction IDs that might be set.
                var updateResult = command.ExecuteNonQueryWithRetry(
                    GetUpdateSqlCommandText(
                        new[]
                        {
                            // Note: We change the row ID to a new ID so we know which row to read (but leave the enqueue row ID)
                            (QueueTableRowIdColumn.ColumnName, rowId.ToString()),
                            (QueueTableTransactionIdColumn.ColumnName, string.Empty), // Empty the transaction ID
                            (QueueTableTransactionLeaseColumn.ColumnName, DateTime.UtcNow.AddMilliseconds(_transactionLeaseMs).Ticks.ToString(CultureInfo.InvariantCulture)) // Set a lease
                        },
                        $"WHERE {QueueTableRowIdColumn.ColumnName} IN (SELECT {QueueTableRowIdColumn.ColumnName} FROM [{QueuePath}] WHERE ({QueueTableTransactionIdColumn.ColumnName} = \"\" AND {QueueTableTransactionLeaseColumn.ColumnName} < {DateTime.UtcNow.Ticks}) OR {QueueTableTransactionIdColumn.ColumnName} = \"{transaction.TransactionId}\" ORDER BY {QueueTableEnqueueTimeColumn.ColumnName} ASC LIMIT 1)"));

                // Check if we actually modified any rows. If not we don't have any queue messages to process.
                // If we did modify a row we can read it and return the result to the caller
                if (updateResult != 1)
                {
                    throw new MessageQueueReadException($"There is nothing to read from the SQLite table: {QueuePath}.");
                }

                // If we did we attempt to update any row - read the updated row directly and deserialize the result.
                // Execute scalar and de-serialize the first column to the expected data type.
                result = command.ExecuteScalarWithRetry<T>(GetReadRowCommandText(rowId, QueueTableRowIdColumn.ColumnName));
            }

            // Create a new command when the transaction is commited to delete this row.
            transaction.EnqueueCommitCommand(GetDeleteRowSqlCommandText(rowId, QueueTableRowIdColumn.ColumnName));

            // Enqueue a function to renew the lease for this row on a timer task.
            transaction.EnqueueRenewLeaseCommand(
                () => GetUpdateSqlCommandText(
                        new[] { (QueueTableTransactionLeaseColumn.ColumnName, DateTime.UtcNow.AddMilliseconds(_transactionLeaseMs).Ticks.ToString(CultureInfo.InvariantCulture)) },
                        GetWhereClauseRowIdCommandText(rowId, QueueTableRowIdColumn.ColumnName)));

            // Create a new command to expire the lease if the transaction is aborted.
            transaction.EnqueueAbortCommand(
                GetUpdateSqlCommandText(
                    new[] { (QueueTableTransactionLeaseColumn.ColumnName, "0") },
                    GetWhereClauseRowIdCommandText(rowId, QueueTableRowIdColumn.ColumnName)));

            return result;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of all managed resources.
        /// </summary>
        /// <param name="disposing">If we are disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        /// <summary>
        /// Gets the SQLite command text to read one row using its row identifier.
        /// </summary>
        /// <param name="rowId">The row identifier.</param>
        /// <param name="columnName">The column name to match the row identifier with.</param>
        /// <returns>The SQLite command text.</returns>
        private string GetReadRowCommandText(Guid rowId, string columnName)
        {
            return $"SELECT * FROM [{QueuePath}] {GetWhereClauseRowIdCommandText(rowId, columnName)}";
        }

        /// <summary>
        /// Get the SQLite command text to update rows with the specified column values.
        /// </summary>
        /// <param name="columnValues">The column name/ values tuple to set during the update.</param>
        /// <param name="whereClause">The where clause to limit which rows are updated.</param>
        /// <returns>The SQLite command text.</returns>
        private string GetUpdateSqlCommandText((string ColumnName, string Value)[] columnValues, string whereClause = "")
        {
            return $"UPDATE [{QueuePath}] " +
                   $"SET {string.Join(", ", columnValues.Select(x => $"{x.ColumnName} = \"{x.Value}\""))} " +
                   $"{(string.IsNullOrWhiteSpace(whereClause) ? string.Empty : whereClause)}";
        }

        /// <summary>
        /// Gets the SQLite command text to delete a specific row.
        /// </summary>
        /// <param name="rowId">The unique row identifier to update.</param>
        /// <param name="columnName">The column name to match the row identifier with.</param>
        /// <returns>The SQLite command text.</returns>
        private string GetDeleteRowSqlCommandText(Guid rowId, string columnName)
        {
            return $"DELETE FROM [{QueuePath}] {GetWhereClauseRowIdCommandText(rowId, columnName)}";
        }

        /// <summary>
        /// Gets the where clause of a SQLite command text for finding a row by its identifier.
        /// </summary>
        /// <param name="rowId">The row identifier to limit the search on.</param>
        /// <param name="columnName">The column name to match the row identifier with.</param>
        /// <returns>The where clause SQLite command text.</returns>
        private string GetWhereClauseRowIdCommandText(Guid rowId, string columnName)
        {
            return $"WHERE {columnName} IN (SELECT {columnName} FROM [{QueuePath}] WHERE {columnName} = \"{rowId}\" LIMIT 1)";
        }

    }
}