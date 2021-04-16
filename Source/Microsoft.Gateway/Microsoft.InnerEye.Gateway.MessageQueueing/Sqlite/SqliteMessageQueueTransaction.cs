namespace Microsoft.InnerEye.Gateway.MessageQueueing.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;

    using Microsoft.Data.Sqlite;
    using Microsoft.InnerEye.Gateway.MessageQueueing.Exceptions;
    using Microsoft.InnerEye.Gateway.Sqlite.Extensions;

    /// <summary>
    /// SQLite queue transaction implementation.
    /// </summary>
    public class SqliteMessageQueueTransaction : IQueueTransaction
    {
        /// <summary>
        /// Error message when Abort or Commit is called before Begin transaction.
        /// </summary>
        private const string BeginNotCalledErrorExceptionMessage = "Begin has not been called yet";

        /// <summary>
        /// The lock object for adding transaction commit/ abort commands.
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// The commands to run on a transaction commit.
        /// </summary>
        private readonly IList<string> _commitCommandTexts = new List<string>();

        /// <summary>
        /// The commands to run on a transaction abort.
        /// </summary>
        private readonly IList<string> _abortCommandTexts = new List<string>();

        /// <summary>
        /// The commands to run to renew the lease of modified rows for this transaction.
        /// </summary>
        private readonly IList<Func<string>> _renewLeaseCommandTexts = new List<Func<string>>();

        /// <summary>
        /// The SQLite connection string.
        /// </summary>
        private readonly string _sqliteConnectionString;

        /// <summary>
        /// The time in milliseconds the transaction should attempt to renew leases.
        /// </summary>
        private readonly uint _transactionRenewLeaseMs;

        /// <summary>
        /// If this instance is disposed.
        /// </summary>
        private bool _disposed = false;

        /// <summary>
        /// The current SQLite connection for this transaction.
        /// </summary>
        private SqliteConnection _sqliteConnection;

        /// <summary>
        /// The renew lease timer (or null if no transaction has started).
        /// </summary>
        private Timer _renewLeaseTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteMessageQueueTransaction"/> class.
        /// </summary>
        /// <param name="sqlConnectionString">The SQLite connection string.</param>
        /// <param name="transactionRenewLeaseMs">The time in milliseconds the transaction should attempt to renew leases.</param>
        public SqliteMessageQueueTransaction(string sqlConnectionString, uint transactionRenewLeaseMs)
        {
            _sqliteConnectionString = sqlConnectionString;
            _transactionRenewLeaseMs = transactionRenewLeaseMs;

            TransactionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// The unqiue identifier for this transaction.
        /// </summary>
        internal string TransactionId { get; }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">When this method is called before begin.</exception>
        /// <exception cref="ObjectDisposedException">If this instance is already disposed.</exception>
        public void Abort()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SqliteMessageQueueTransaction));
            }

            if (_sqliteConnection == null)
            {
                throw new InvalidOperationException(BeginNotCalledErrorExceptionMessage);
            }

            // Lock to access any abort commands and to make sure no renew tasks are executing during abort.
            lock (_lockObject)
            {
                // First, stop any renew lease tasks.
                DisposeRenewLeaseTimer();

                // Execute any sqlite commands required to be executed on an abort and clear the collections.
                ExecuteSqliteCommands(_abortCommandTexts, true);

                // Dispose of the current SQLite connection
                DisposeCurrentConnection();
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">If begin has already been called for this transaction.</exception>
        /// <exception cref="MessageQueueTransactionBeginException">Failed to begin the transaction.</exception>
        /// <exception cref="ObjectDisposedException">If this instance is already disposed.</exception>
        public void Begin()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SqliteMessageQueueTransaction));
            }

            if (_sqliteConnection != null)
            {
                throw new InvalidOperationException("Begin has already been called");
            }

            // Create a new SQLite connection to the database (we create a new connection per transaction)
            _sqliteConnection = new SqliteConnection(_sqliteConnectionString);

            try
            {
                _sqliteConnection.Open();
            }
            catch (SqliteException e)
            {
                throw new MessageQueueTransactionBeginException("Failed to start transaction. Could not open connection to database.", e);
            }
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">When this method is called before begin.</exception>
        /// <exception cref="ObjectDisposedException">If this instance is already disposed.</exception>
        public void Commit()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SqliteMessageQueueTransaction));
            }

            if (_sqliteConnection == null)
            {
                throw new InvalidOperationException(BeginNotCalledErrorExceptionMessage);
            }

            // Lock to access any commit commands and to make sure no renew tasks are executing during commit.
            lock (_lockObject)
            {
                // First, stop any renew lease tasks.
                DisposeRenewLeaseTimer();

                // Execute any sqlite commands required to be executed on a commit and clear the collections.
                ExecuteSqliteCommands(_commitCommandTexts, true);

                // Dispose of the current SQLite connection
                DisposeCurrentConnection();
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Gets a SQLite command for this transaction.
        /// </summary>
        /// <param name="commandType">The command type.</param>
        /// <param name="sqliteTransaction">The SQLite transaction for this command or null.</param>
        /// <returns>The SQLite command</returns>
        /// <exception cref="InvalidOperationException">When this method is called before begin.</exception>
        internal SqliteCommand GetSqliteCommand(CommandType commandType = CommandType.Text, SqliteTransaction sqliteTransaction = null)
        {
            if (_sqliteConnection == null)
            {
                throw new InvalidOperationException(BeginNotCalledErrorExceptionMessage);
            }

            return new SqliteCommand(string.Empty, _sqliteConnection) { CommandType = commandType, Transaction = sqliteTransaction };
        }

        /// <summary>
        /// Enqueues a SQLite command to the commit queue.
        /// </summary>
        /// <param name="sqliteCommandText">The SQL command text to run on a transaction commit.</param>
        internal void EnqueueCommitCommand(string sqliteCommandText)
        {
            lock (_lockObject)
            {
                _commitCommandTexts.Add(sqliteCommandText);
            }
        }

        /// <summary>
        /// Enqueues a SQLite command to the abort queue.
        /// </summary>
        /// <param name="sqliteCommandText">The SQL command text to run on a transaction abort.</param>
        internal void EnqueueAbortCommand(string sqliteCommandText)
        {
            lock (_lockObject)
            {
                _abortCommandTexts.Add(sqliteCommandText);
            }
        }

        /// <summary>
        /// Enqueues a SQLite command to the abort queue.
        /// </summary>
        /// <param name="sqliteCommandText">The SQL command text to run on a transaction abort.</param>
        /// <exception cref="InvalidOperationException">When this method is called before begin.</exception>
        internal void EnqueueRenewLeaseCommand(Func<string> getSqliteCommandTextFunc)
        {
            // We cannot enqueue a renew lease task if the transaction has not started.
            if (_sqliteConnection == null)
            {
                throw new InvalidOperationException(BeginNotCalledErrorExceptionMessage);
            }

            lock (_lockObject)
            {
                _renewLeaseCommandTexts.Add(getSqliteCommandTextFunc);

                // Create a timer object that attempts to renew the lease. Only create if we haven't created one yet
                // Note: We only create the timer when we enqueue a task as a performance optimisation.
                if (_renewLeaseTimer == null)
                {
                    _renewLeaseTimer = new Timer(ExecuteRenewLeaseCommandTexts, null, _transactionRenewLeaseMs, _transactionRenewLeaseMs);
                }
            }
        }

        /// <summary>
        /// Disposes of all managed resources. Also calls Abort() if the transaction is still open.
        /// </summary>
        /// <param name="disposing">If we are disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Try to abort the connection if we still have an open transaction
                if (_sqliteConnection != null)
                {
                    Abort();
                }

                // Clear any timer tasks
                DisposeRenewLeaseTimer();

                // Clear any open connection
                DisposeCurrentConnection();

                // Clear post transaction commands.
                ClearPostTransactionCommands();
            }

            _disposed = true;
        }

        /// <summary>
        /// Disposes of the renew lease timer.
        /// </summary>
        private void DisposeRenewLeaseTimer()
        {
            if (_renewLeaseTimer != null)
            {
                _renewLeaseTimer.Dispose();
                _renewLeaseTimer = null;
            }
        }

        /// <summary>
        /// Disposes of the current SQLite connection.
        /// </summary>
        private void DisposeCurrentConnection()
        {
            if (_sqliteConnection != null)
            {
                _sqliteConnection.Dispose();
                _sqliteConnection = null;
            }
        }

        /// <summary>
        /// Clears of all abort/ commit/ renew lease commands
        /// </summary>
        private void ClearPostTransactionCommands()
        {
            lock (_lockObject)
            {
                _abortCommandTexts.Clear();
                _commitCommandTexts.Clear();
                _renewLeaseCommandTexts.Clear();
            }
        }

        /// <summary>
        /// Execute all the current SQLite commands in the renew tasks.
        /// </summary>
        /// <param name="timerStateInformation">The state information of the timer object.</param>
        private void ExecuteRenewLeaseCommandTexts(object timerStateInformation)
        {
            // Make sure we lock access for the time required to execute all renew commands.
            // We want to make sure abort/ commit is not called whilst we are processing renew tasks; this could put the DB in an invalid state.
            lock (_lockObject)
            {
                ExecuteSqliteCommands(_renewLeaseCommandTexts.Select(x => x()), clearCommandTexts: false);
            }
        }

        /// <summary>
        /// Executes all the SQLite command texts as non query commands.
        /// </summary>
        /// <param name="commandTexts">The SQLite command texts to execute.</param>
        /// <param name="clearCommandTexts">If we should clear all the command texts after the SQLite commands have been executed.</param>
        /// <exception cref="InvalidOperationException">When this method is called before begin.</exception>
        private void ExecuteSqliteCommands(IEnumerable<string> commandTexts, bool clearCommandTexts)
        {
            if (_sqliteConnection == null)
            {
                throw new InvalidOperationException(BeginNotCalledErrorExceptionMessage);
            }

            // Don't catch exceptions - we want to abort the transaction if any of these commands fail.
            foreach (var commandText in commandTexts)
            {
                using (var sqliteCommand = GetSqliteCommand())
                {
                    var result = sqliteCommand.ExecuteNonQueryWithRetry(commandText);
                }
            }

            if (clearCommandTexts)
            {
                ClearPostTransactionCommands();
            }
        }
    }
}