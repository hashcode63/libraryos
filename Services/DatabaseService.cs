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

        public NpgsqlConnection GetConnection() => new NpgsqlConnection(_connectionString);

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
        /// Initialize database tables — FIX: includes ALL book columns + indexes
        /// </summary>
        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                // FIX: Added all extended book columns so model and DB stay in sync
                string createBooksTable = @"
                    CREATE TABLE IF NOT EXISTS books (
                        id               SERIAL PRIMARY KEY,
                        title            VARCHAR(255) NOT NULL,
                        author           VARCHAR(255) NOT NULL,
                        isbn             VARCHAR(20)  UNIQUE NOT NULL,
                        category         VARCHAR(100),
                        book_link        TEXT,
                        is_available     BOOLEAN   DEFAULT true,
                        uploaded_by      INTEGER,
                        upload_status    VARCHAR(20) DEFAULT 'approved',
                        description      TEXT,
                        publisher        VARCHAR(255),
                        publication_year INTEGER,
                        pages            INTEGER,
                        language         VARCHAR(50),
                        cover_image_url  TEXT,
                        created_at       TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";

                using (var cmd = new NpgsqlCommand(createBooksTable, conn))
                    await cmd.ExecuteNonQueryAsync();

                string createPatronsTable = @"
                    CREATE TABLE IF NOT EXISTS patrons (
                        id                SERIAL PRIMARY KEY,
                        name              VARCHAR(255) NOT NULL,
                        email             VARCHAR(255) UNIQUE NOT NULL,
                        phone             VARCHAR(20),
                        registration_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        max_books         INTEGER DEFAULT 3,
                        is_active         BOOLEAN DEFAULT true
                    )";

                using (var cmd = new NpgsqlCommand(createPatronsTable, conn))
                    await cmd.ExecuteNonQueryAsync();

                string createLoansTable = @"
                    CREATE TABLE IF NOT EXISTS loans (
                        id            SERIAL PRIMARY KEY,
                        book_id       INTEGER NOT NULL REFERENCES books(id),
                        patron_id     INTEGER NOT NULL REFERENCES patrons(id),
                        borrowed_date DATE NOT NULL,
                        due_date      DATE NOT NULL,
                        return_date   DATE,
                        is_returned   BOOLEAN DEFAULT false,
                        borrow_fee    DECIMAL(10,2) DEFAULT 100.00,
                        overdue_fine  DECIMAL(10,2) DEFAULT 0.00,
                        created_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    )";

                using (var cmd = new NpgsqlCommand(createLoansTable, conn))
                    await cmd.ExecuteNonQueryAsync();

                // FIX: Create indexes here so they exist even if schema SQL was never run manually
                string[] indexes =
                {
                    "CREATE INDEX IF NOT EXISTS idx_books_isbn      ON books(isbn)",
                    "CREATE INDEX IF NOT EXISTS idx_books_available ON books(is_available)",
                    "CREATE INDEX IF NOT EXISTS idx_patrons_email   ON patrons(email)",
                    "CREATE INDEX IF NOT EXISTS idx_loans_patron    ON loans(patron_id)",
                    "CREATE INDEX IF NOT EXISTS idx_loans_book      ON loans(book_id)",
                    "CREATE INDEX IF NOT EXISTS idx_loans_returned  ON loans(is_returned)",
                };

                foreach (var idx in indexes)
                    using (var cmd = new NpgsqlCommand(idx, conn))
                        await cmd.ExecuteNonQueryAsync();

                Console.WriteLine("✅ Database tables and indexes initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, params NpgsqlParameter[] parameters)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<NpgsqlDataReader> ExecuteReaderAsync(string query, params NpgsqlParameter[] parameters)
        {
            var conn = GetConnection();
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
        }

        public async Task<object?> ExecuteScalarAsync(string query, params NpgsqlParameter[] parameters)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(query, conn);
            if (parameters != null) cmd.Parameters.AddRange(parameters);
            return await cmd.ExecuteScalarAsync();
        }
    }
}
