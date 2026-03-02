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
        /// Add a new book to the library — FIX: returns the new ID via RETURNING id
        /// </summary>
        public async Task<(bool success, int newId)> AddBookAsync(Book book)
        {
            try
            {
                // FIX: Added all missing columns (description, publisher, publication_year, pages, language, cover_image_url)
                // FIX: Used RETURNING id so the caller always knows the new book's ID
                string query = @"
                    INSERT INTO books 
                        (title, author, isbn, category, book_link, uploaded_by, upload_status,
                         description, publisher, publication_year, pages, language, cover_image_url)
                    VALUES 
                        (@title, @author, @isbn, @category, @link, @uploaded_by, @status,
                         @description, @publisher, @pub_year, @pages, @language, @cover)
                    RETURNING id";

                var parameters = new[]
                {
                    new NpgsqlParameter("@title",       book.Title),
                    new NpgsqlParameter("@author",      book.Author),
                    new NpgsqlParameter("@isbn",        book.ISBN),
                    new NpgsqlParameter("@category",    book.Category     ?? (object)DBNull.Value),
                    new NpgsqlParameter("@link",        book.BookLink     ?? (object)DBNull.Value),
                    new NpgsqlParameter("@uploaded_by", book.UploadedBy   ?? (object)DBNull.Value),
                    new NpgsqlParameter("@status",      book.UploadStatus),
                    new NpgsqlParameter("@description", book.Description  ?? (object)DBNull.Value),
                    new NpgsqlParameter("@publisher",   book.Publisher    ?? (object)DBNull.Value),
                    new NpgsqlParameter("@pub_year",    book.PublicationYear ?? (object)DBNull.Value),
                    new NpgsqlParameter("@pages",       book.Pages        ?? (object)DBNull.Value),
                    new NpgsqlParameter("@language",    book.Language     ?? (object)DBNull.Value),
                    new NpgsqlParameter("@cover",       book.CoverImageUrl ?? (object)DBNull.Value),
                };

                var result = await _db.ExecuteScalarAsync(query, parameters);
                if (result != null)
                {
                    int newId = Convert.ToInt32(result);
                    book.Id = newId;
                    return (true, newId);
                }
                return (false, 0);
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                Console.WriteLine($"❌ A book with this ISBN already exists! Please use a unique ISBN.");
                return (false, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error adding book: {ex.Message}");
                return (false, 0);
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
                    books.Add(MapReaderToBook(reader));
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
                    return MapReaderToBook(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching book: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Search books by title, author, or category
        /// </summary>
        public async Task<List<Book>> SearchBooksAsync(string keyword)
        {
            var books = new List<Book>();
            try
            {
                string query = @"
                    SELECT * FROM books 
                    WHERE LOWER(title)    LIKE LOWER(@keyword) 
                       OR LOWER(author)   LIKE LOWER(@keyword)
                       OR LOWER(category) LIKE LOWER(@keyword)
                    ORDER BY title";

                var parameters = new[] { new NpgsqlParameter("@keyword", $"%{keyword}%") };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);
                while (await reader.ReadAsync())
                    books.Add(MapReaderToBook(reader));
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
            catch { return false; }
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
                string checkQuery = "SELECT COUNT(*) FROM loans WHERE book_id = @id AND is_returned = false";
                var checkParams = new[] { new NpgsqlParameter("@id", bookId) };
                var activeLoans = await _db.ExecuteScalarAsync(checkQuery, checkParams);

                if (activeLoans != null && Convert.ToInt32(activeLoans) > 0)
                {
                    Console.WriteLine("❌ Cannot remove book — it currently has active loans. Return the book first.");
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
            catch { return 0; }
        }

        /// <summary>
        /// Map database reader to Book object — FIX: now includes all extended columns safely
        /// </summary>
        private Book MapReaderToBook(NpgsqlDataReader reader)
        {
            // Helper to safely get optional string columns
            string? SafeString(string col)
            {
                int ord = reader.GetOrdinal(col);
                return reader.IsDBNull(ord) ? null : reader.GetString(ord);
            }
            int? SafeInt(string col)
            {
                int ord = reader.GetOrdinal(col);
                return reader.IsDBNull(ord) ? null : reader.GetInt32(ord);
            }

            return new Book
            {
                Id             = reader.GetInt32(reader.GetOrdinal("id")),
                Title          = reader.GetString(reader.GetOrdinal("title")),
                Author         = reader.GetString(reader.GetOrdinal("author")),
                ISBN           = reader.GetString(reader.GetOrdinal("isbn")),
                Category       = SafeString("category"),
                BookLink       = SafeString("book_link"),
                IsAvailable    = reader.GetBoolean(reader.GetOrdinal("is_available")),
                UploadedBy     = SafeInt("uploaded_by"),
                UploadStatus   = reader.GetString(reader.GetOrdinal("upload_status")),
                CreatedAt      = reader.GetDateTime(reader.GetOrdinal("created_at")),
                Description    = SafeString("description"),
                Publisher      = SafeString("publisher"),
                PublicationYear = SafeInt("publication_year"),
                Pages          = SafeInt("pages"),
                Language       = SafeString("language"),
                CoverImageUrl  = SafeString("cover_image_url"),
            };
        }
    }
}
