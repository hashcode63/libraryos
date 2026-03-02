using LibraryOS.Models;
using Npgsql;

namespace LibraryOS.Services
{
    /// <summary>
    /// Service for managing book operations
    /// </summary>
    public class BookService
    {
        private readonly DatabaseService _db;

        public BookService(DatabaseService db)
        {
            _db = db;
        }

        /// <summary>
        /// Add a new book to the library
        /// </summary>
        public async Task<bool> AddBookAsync(Book book)
        {
            try
            {
                string query = @"
                    INSERT INTO books (title, author, isbn, category, book_link, uploaded_by, upload_status)
                    VALUES (@title, @author, @isbn, @category, @link, @uploaded_by, @status)";

                var parameters = new[]
                {
                    new NpgsqlParameter("@title", book.Title),
                    new NpgsqlParameter("@author", book.Author),
                    new NpgsqlParameter("@isbn", book.ISBN),
                    new NpgsqlParameter("@category", book.Category ?? (object)DBNull.Value),
                    new NpgsqlParameter("@link", book.BookLink ?? (object)DBNull.Value),
                    new NpgsqlParameter("@uploaded_by", book.UploadedBy ?? (object)DBNull.Value),
                    new NpgsqlParameter("@status", book.UploadStatus)
                };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding book: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all books from the library
        /// </summary>
        public async Task<List<Book>> GetAllBooksAsync()
        {
            var books = new List<Book>();

            try
            {
                string query = "SELECT * FROM books ORDER BY id";
                using var reader = await _db.ExecuteReaderAsync(query);

                while (await reader.ReadAsync())
                {
                    books.Add(MapReaderToBook(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching books: {ex.Message}");
            }

            return books;
        }

        /// <summary>
        /// Get a book by ID
        /// </summary>
        public async Task<Book?> GetBookByIdAsync(int id)
        {
            try
            {
                string query = "SELECT * FROM books WHERE id = @id";
                var parameters = new[] { new NpgsqlParameter("@id", id) };

                using var reader = await _db.ExecuteReaderAsync(query, parameters);

                if (await reader.ReadAsync())
                {
                    return MapReaderToBook(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching book: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Search books by title or author
        /// </summary>
        public async Task<List<Book>> SearchBooksAsync(string keyword)
        {
            var books = new List<Book>();

            try
            {
                string query = @"
                    SELECT * FROM books 
                    WHERE LOWER(title) LIKE LOWER(@keyword) 
                       OR LOWER(author) LIKE LOWER(@keyword)
                    ORDER BY title";

                var parameters = new[] { new NpgsqlParameter("@keyword", $"%{keyword}%") };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);

                while (await reader.ReadAsync())
                {
                    books.Add(MapReaderToBook(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error searching books: {ex.Message}");
            }

            return books;
        }

        /// <summary>
        /// Check if a book is available
        /// </summary>
        public async Task<bool> IsBookAvailableAsync(int bookId)
        {
            try
            {
                string query = "SELECT is_available FROM books WHERE id = @id";
                var parameters = new[] { new NpgsqlParameter("@id", bookId) };

                var result = await _db.ExecuteScalarAsync(query, parameters);
                return result != null && (bool)result;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Update book availability
        /// </summary>
        public async Task<bool> UpdateAvailabilityAsync(int bookId, bool isAvailable)
        {
            try
            {
                string query = "UPDATE books SET is_available = @available WHERE id = @id";
                var parameters = new[]
                {
                    new NpgsqlParameter("@available", isAvailable),
                    new NpgsqlParameter("@id", bookId)
                };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating availability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Remove a book from the library
        /// </summary>
        public async Task<bool> RemoveBookAsync(int bookId)
        {
            try
            {
                // First check if book has active loans
                string checkQuery = "SELECT COUNT(*) FROM loans WHERE book_id = @id AND is_returned = false";
                var checkParams = new[] { new NpgsqlParameter("@id", bookId) };
                var activeLoans = await _db.ExecuteScalarAsync(checkQuery, checkParams);

                if (activeLoans != null && Convert.ToInt32(activeLoans) > 0)
                {
                    Console.WriteLine("❌ Cannot remove book with active loans!");
                    return false;
                }

                string query = "DELETE FROM books WHERE id = @id";
                var parameters = new[] { new NpgsqlParameter("@id", bookId) };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error removing book: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get available books count
        /// </summary>
        public async Task<int> GetAvailableBooksCountAsync()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM books WHERE is_available = true";
                var result = await _db.ExecuteScalarAsync(query);
                return result != null ? Convert.ToInt32(result) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Map database reader to Book object
        /// </summary>
        private Book MapReaderToBook(NpgsqlDataReader reader)
        {
            return new Book
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Author = reader.GetString(reader.GetOrdinal("author")),
                ISBN = reader.GetString(reader.GetOrdinal("isbn")),
                Category = reader.IsDBNull(reader.GetOrdinal("category")) ? null : reader.GetString(reader.GetOrdinal("category")),
                BookLink = reader.IsDBNull(reader.GetOrdinal("book_link")) ? null : reader.GetString(reader.GetOrdinal("book_link")),
                IsAvailable = reader.GetBoolean(reader.GetOrdinal("is_available")),
                UploadedBy = reader.IsDBNull(reader.GetOrdinal("uploaded_by")) ? null : reader.GetInt32(reader.GetOrdinal("uploaded_by")),
                UploadStatus = reader.GetString(reader.GetOrdinal("upload_status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };
        }
    }
}
