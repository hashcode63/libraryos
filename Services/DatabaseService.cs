using Npgsql;
using System.Data;

namespace LibraryOS.Services
{
    /// <summary>
    /// Handles database connection and operations for Neon PostgreSQL
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Get a new database connection
        /// </summary>
        public NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();
                return conn.State == ConnectionState.Open;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Initialize database tables if they don't exist
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                // Create Books table
                string createBooksTable = @"
                    CREATE TABLE IF NOT EXISTS books (
                        id SERIAL PRIMARY KEY,
                        title VARCHAR(255) NOT NULL,
                        author VARCHAR(255) NOT NULL,
                        isbn VARCHAR(20) UNIQUE NOT NULL,
                        category VARCHAR(100),
                        book_link TEXT,
                        is_available BOOLEAN DEFAULT true,
                        uploaded_by INTEGER,
                        upload_status VARCHAR(20) DEFAULT 'approved',
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";

                using (var cmd = new NpgsqlCommand(createBooksTable, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Create Patrons table
                string createPatronsTable = @"
                    CREATE TABLE IF NOT EXISTS patrons (
                        id SERIAL PRIMARY KEY,
                        name VARCHAR(255) NOT NULL,
                        email VARCHAR(255) UNIQUE NOT NULL,
                        phone VARCHAR(20),
                        registration_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        max_books INTEGER DEFAULT 3,
                        is_active BOOLEAN DEFAULT true
                    )";

                using (var cmd = new NpgsqlCommand(createPatronsTable, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Create Loans table
                string createLoansTable = @"
                    CREATE TABLE IF NOT EXISTS loans (
                        id SERIAL PRIMARY KEY,
                        book_id INTEGER NOT NULL REFERENCES books(id),
                        patron_id INTEGER NOT NULL REFERENCES patrons(id),
                        borrowed_date DATE NOT NULL,
                        due_date DATE NOT NULL,
                        return_date DATE,
                        is_returned BOOLEAN DEFAULT false,
                        borrow_fee DECIMAL(10, 2) DEFAULT 100.00,
                        overdue_fine DECIMAL(10, 2) DEFAULT 0.00,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";

                using (var cmd = new NpgsqlCommand(createLoansTable, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                Console.WriteLine("✅ Database tables initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Execute a non-query command (INSERT, UPDATE, DELETE)
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(string query, params NpgsqlParameter[] parameters)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(query, conn);
            
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            return await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Execute a query and return a data reader
        /// </summary>
        public async Task<NpgsqlDataReader> ExecuteReaderAsync(string query, params NpgsqlParameter[] parameters)
        {
            var conn = GetConnection();
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(query, conn);
            
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        /// <summary>
        /// Execute a scalar query (returns single value)
        /// </summary>
        public async Task<object?> ExecuteScalarAsync(string query, params NpgsqlParameter[] parameters)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(query, conn);
            
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);

            return await cmd.ExecuteScalarAsync();
        }
    }
}
