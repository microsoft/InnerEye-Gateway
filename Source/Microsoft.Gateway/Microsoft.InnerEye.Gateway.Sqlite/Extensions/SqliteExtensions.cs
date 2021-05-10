namespace Microsoft.InnerEye.Gateway.Sqlite.Extensions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using Microsoft.Data.Sqlite;
    using Microsoft.InnerEye.Gateway.Sqlite.Exceptions;
    using Newtonsoft.Json;

    /// <summary>
    /// Extension methods for SQLite.
    /// </summary>
    public static class SqliteExtensions
    {
        /// <summary>
        /// Exception message format for null or whitespace parameters.
        /// </summary>
        private const string NullOrWhitespaceParameterExceptionMessageFormat = "{0} is null or whitespace.";

        /// <summary>
        /// Executes a non-transactional non-query against the specified database on a new database connection.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the database.</param>
        /// <param name="commandText">The non-query SQLite command text to issue.</param>
        /// <returns>The number of rows modified by the non-query.</returns>
        /// <exception cref="ArgumentException">The connection string or command text is null or whitespace.</exception>
        /// <exception cref="SqliteException">If we fail to open a connection to the database.</exception>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static int ExecuteNonQueryNewConnection(string connectionString, string commandText)
        {
            connectionString = string.IsNullOrWhiteSpace(connectionString) ? throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, NullOrWhitespaceParameterExceptionMessageFormat, nameof(connectionString)), nameof(connectionString)) : connectionString;
            commandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, NullOrWhitespaceParameterExceptionMessageFormat, nameof(commandText)), nameof(commandText)) : commandText;

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqliteCommand(commandText, connection))
                {
                    // Use the extension method to execute the non query (this handles retry logic)
                    return command.ExecuteNonQueryWithRetry(commandText);
                }
            }
        }

        /// <summary>
        /// Executes a non query using the command and command text.
        /// </summary>
        /// <param name="command">The command to execute the non query.</param>
        /// <param name="commandText">The command text to issue.</param>
        /// <param name="retryCount">The number of times the method will attempt to execute the non-query on SQLite failures.</param>
        /// <returns>The number of rows modified by the non-query.</returns>
        /// <exception cref="ArgumentException">If the command text is null or white space.</exception>
        /// <exception cref="ArgumentNullException">If the SQLite command is null.</exception>
        /// <exception cref="MessageQueueWriteException">Failed to execute the non query.</exception>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static int ExecuteNonQueryWithRetry(this SqliteCommand command, string commandText, int retryCount = 3)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.CommandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, NullOrWhitespaceParameterExceptionMessageFormat, nameof(commandText)), nameof(commandText)) : commandText;

            try
            {
                return command.ExecuteNonQuery();
            }
            // Only catch SQLite exceptions
            catch (SqliteException e)
            {
                if (retryCount == 0)
                {
                    throw new SqliteWriteException($"[SQLite ExecuteNonQuery] Failed to execute non query: {commandText}. Exception: {e.Message}", e);
                }
            }

            // Retry if we have the ability to try again.
            return ExecuteNonQueryWithRetry(command, commandText, retryCount - 1);
        }

        /// <summary>
        /// Executes a scalar against the SQLite command and de-serializes the result to type T.
        /// </summary>
        /// <typeparam name="T">The type to de-serialize the result to.</typeparam>
        /// <param name="command">The SQLite command to execute the non-scalar against.</param>
        /// <param name="commandText">The command text to run.</param>
        /// <param name="retryCount">The number of times the method will attempt to execute the scalar on SQLite failures.</param>
        /// <returns>The de-serializes first row/ first column from the command.</returns>
        /// <exception cref="ArgumentException">If the command text is null or white space.</exception>
        /// <exception cref="ArgumentNullException">If the SQLite command is null.</exception>
        /// <exception cref="MessageQueueReadException">If we fail to read anything from the first row/ column.</exception>
        [SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static T ExecuteScalarWithRetry<T>(this SqliteCommand command, string commandText, int retryCount = 3)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.CommandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, NullOrWhitespaceParameterExceptionMessageFormat, nameof(commandText)), nameof(commandText)) : commandText;

            try
            {
                var result = command.ExecuteScalar();

                if (result == null)
                {
                    throw new SqliteReadException($"Failed to execute scalar command: {commandText}.");
                }

                return JsonConvert.DeserializeObject<T>(Convert.ToString(result, CultureInfo.InvariantCulture));
            }
            // Only catch SQLite exceptions
            catch (SqliteException e)
            {
                if (retryCount == 0)
                {
                    throw new SqliteReadException($"[SQLite ExecuteNonQuery] Failed to execute scalar command: {commandText}. Exception: {e.Message}", e);
                }
            }

            return ExecuteScalarWithRetry<T>(command, commandText, retryCount - 1);
        }
    }
}
