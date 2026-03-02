using LibraryOS.Models;
using Npgsql;

namespace LibraryOS.Services
{
    /// <summary>
    /// Service for managing patron operations
    /// </summary>
    public class PatronService
    {
        private readonly DatabaseService _db;

        public PatronService(DatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// Register a new patron
        /// </summary>
        public async Task<bool> RegisterPatronAsync(Patron patron)
        {
            try
            {
                string query = @"
                    INSERT INTO patrons (name, email, phone, max_books)
                    VALUES (@name, @email, @phone, @max_books)";

                var parameters = new[]
                {
                    new NpgsqlParameter("@name", patron.Name),
                    new NpgsqlParameter("@email", patron.Email),
                    new NpgsqlParameter("@phone", patron.Phone ?? (object)DBNull.Value),
                    new NpgsqlParameter("@max_books", patron.MaxBooks)
                };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error registering patron: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all patrons
        /// </summary>
        public async Task<List<Patron>> GetAllPatronsAsync()
        {
            var patrons = new List<Patron>();

            try
            {
                string query = "SELECT * FROM patrons ORDER BY id";
                using var reader = await _db.ExecuteReaderAsync(query);

                while (await reader.ReadAsync())
                {
                    patrons.Add(MapReaderToPatron(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching patrons: {ex.Message}");
            }

            return patrons;
        }

        /// <summary>
        /// Get a patron by ID
        /// </summary>
        public async Task<Patron?> GetPatronByIdAsync(int id)
        {
            try
            {
                string query = "SELECT * FROM patrons WHERE id = @id";
                var parameters = new[] { new NpgsqlParameter("@id", id) };

                using var reader = await _db.ExecuteReaderAsync(query, parameters);

                if (await reader.ReadAsync())
                {
                    return MapReaderToPatron(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching patron: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get patron by email
        /// </summary>
        public async Task<Patron?> GetPatronByEmailAsync(string email)
        {
            try
            {
                string query = "SELECT * FROM patrons WHERE email = @email";
                var parameters = new[] { new NpgsqlParameter("@email", email) };

                using var reader = await _db.ExecuteReaderAsync(query, parameters);

                if (await reader.ReadAsync())
                {
                    return MapReaderToPatron(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching patron: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get count of books currently borrowed by patron
        /// </summary>
        public async Task<int> GetBorrowedBooksCountAsync(int patronId)
        {
            try
            {
                string query = "SELECT COUNT(*) FROM loans WHERE patron_id = @id AND is_returned = false";
                var parameters = new[] { new NpgsqlParameter("@id", patronId) };

                var result = await _db.ExecuteScalarAsync(query, parameters);
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if patron can borrow more books
        /// </summary>
        public async Task<bool> CanBorrowAsync(int patronId)
        {
            try
            {
                var patron = await GetPatronByIdAsync(patronId);
                if (patron == null || !patron.IsActive)
                    return false;

                int currentBorrowedCount = await GetBorrowedBooksCountAsync(patronId);
                return currentBorrowedCount < patron.MaxBooks;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a patron
        /// </summary>
        public async Task<bool> RemovePatronAsync(int patronId)
        {
            try
            {
                // Check for active loans
                string checkQuery = "SELECT COUNT(*) FROM loans WHERE patron_id = @id AND is_returned = false";
                var checkParams = new[] { new NpgsqlParameter("@id", patronId) };
                var activeLoans = await _db.ExecuteScalarAsync(checkQuery, checkParams);

                if (activeLoans != null && Convert.ToInt32(activeLoans) > 0)
                {
                    Console.WriteLine("❌ Cannot remove patron with active loans!");
                    return false;
                }

                string query = "DELETE FROM patrons WHERE id = @id";
                var parameters = new[] { new NpgsqlParameter("@id", patronId) };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error removing patron: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Update patron status
        /// </summary>
        public async Task<bool> UpdatePatronStatusAsync(int patronId, bool isActive)
        {
            try
            {
                string query = "UPDATE patrons SET is_active = @active WHERE id = @id";
                var parameters = new[]
                {
                    new NpgsqlParameter("@active", isActive),
                    new NpgsqlParameter("@id", patronId)
                };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating patron: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get active patrons count
        /// </summary>
        public async Task<int> GetActivePatronsCountAsync()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM patrons WHERE is_active = true";
                var result = await _db.ExecuteScalarAsync(query);
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Map database reader to Patron object
        /// </summary>
        private Patron MapReaderToPatron(NpgsqlDataReader reader)
        {
            return new Patron
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                RegistrationDate = reader.GetDateTime(reader.GetOrdinal("registration_date")),
                MaxBooks = reader.GetInt32(reader.GetOrdinal("max_books")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
            };
        }
    }
}
