namespace Microsoft.InnerEye.Gateway.Sqlite
{
    using System;
    using System.IO;
    using System.Linq;

    using SQLitePCL;

    using Microsoft.Data.Sqlite;
    using Microsoft.InnerEye.Gateway.Sqlite.Extensions;

    public class SqliteManager
    {
        /// <summary>
        /// The database connection path.
        /// Note: To explore the database file you can use the tool: https://sqlitebrowser.org/
        /// </summary>
        private readonly string DatabaseConnectionStringFormat;

        /// <summary>
        /// The connection string to the database.
        /// </summary>
        public readonly string DatabaseConnectionString;

        /// <summary>
        /// The table name.
        /// </summary>
        public readonly string _tableName;

        /// <summary>
        /// Exception message format for null or whitespace parameters.
        /// </summary>
        protected const string NullOrWhitespaceParameterExceptionMessageFormat = "{0} is null or whitespace.";

        /// <summary>
        /// The SQLite command text format for clearing a table of all rows.
        /// </summary>
        public const string ClearTableCommandTextFormat = "DELETE FROM [{0}]";

        /// <summary>
        /// The queue table data column names and data type.
        /// </summary>
        private readonly (string ColumnName, string ColumnDataType)[] Columns;

        public SqliteManager(string tableName, string databaseConnectionStringFormat, (string ColumnName, string ColumnDataType)[] columns)
        {
            _tableName = !string.IsNullOrWhiteSpace(tableName) ? tableName : throw new ArgumentException(nameof(tableName));

            DatabaseConnectionStringFormat = databaseConnectionStringFormat;
            Columns = columns;
            // Get the database connection string (create the local AppData folder if it does not exist)
            DatabaseConnectionString = GetDatabaseConnectionString();

            // Set the SQLite engine provider (we use SQLite3)
            Batteries_V2.Init();
            //raw.SetProvider(new SQLite3Provider_e_sqlite3());

            CreateTableIfNotExists();
        }

        /// <summary>
        /// The SQLite command text format for inserting a row into a table.
        /// </summary>
        public string GetInsertRowCommandFormat()
        {
            return $"INSERT INTO [{_tableName}] ({string.Join(", ", Columns.Select(x => x.ColumnName))}) VALUES({string.Join(", ", Columns.Select(x => $"@{x.ColumnName}"))})";
        }

        /// <summary>
        /// Creates the queue table if it does not exist.
        /// </summary>
        /// <param name="command">The SQLite command the method will use to create the table if it does not exist.</param>
        /// <exception cref="ArgumentException">The connection string or command text is null or whitespace.</exception>
        /// <exception cref="SqliteException">If we fail to open a connection to the database.</exception>
        public void CreateTableIfNotExists()
        {
            string createTableIfNotExistsCommandFormat = "CREATE TABLE IF NOT EXISTS [{0}] " + $"({string.Join(", ", Columns.Select(x => $"{x.ColumnName} {x.ColumnDataType}"))})";

            SqliteExtensions.ExecuteNonQueryNewConnection(
               connectionString: DatabaseConnectionString,
               commandText: string.Format(createTableIfNotExistsCommandFormat, _tableName));
        }

        /// <summary>
        /// Gets the connection string the database.
        /// The database is stored in the Microsoft InnerEye Gateway Log local application data folder.
        /// This folder will be created in this method if it does not exist.
        /// </summary>
        /// <returns>The database connection string.</returns>
        private string GetDatabaseConnectionString()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Microsoft InnerEye Gateway\");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return string.Format(DatabaseConnectionStringFormat, path);
        }
    }
}
