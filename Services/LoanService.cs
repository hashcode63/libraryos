using LibraryOS.Models;
using Npgsql;

namespace LibraryOS.Services
{
    /// <summary>
    /// Service for managing loan operations (borrow/return)
    /// </summary>
    public class LoanService
    {
        private readonly DatabaseService _db;
        private readonly BookService _bookService;
        private readonly PatronService _patronService;

        public LoanService(DatabaseService db, BookService bookService, PatronService patronService)
        {
            _db = db;
            _bookService = bookService;
            _patronService = patronService;
        }

        /// <summary>
        /// Borrow a book
        /// </summary>
        public async Task<(bool success, string message)> BorrowBookAsync(int bookId, int patronId, decimal borrowFee = 100m)
        {
            try
            {
                // Check if book exists and is available
                var book = await _bookService.GetBookByIdAsync(bookId);
                if (book == null)
                    return (false, "❌ Book not found!");

                if (!book.IsAvailable)
                    return (false, "❌ Book is already borrowed!");

                // Check if patron exists and can borrow
                var patron = await _patronService.GetPatronByIdAsync(patronId);
                if (patron == null)
                    return (false, "❌ Patron not found!");

                if (!patron.IsActive)
                    return (false, "❌ Patron account is inactive!");

                bool canBorrow = await _patronService.CanBorrowAsync(patronId);
                if (!canBorrow)
                    return (false, $"❌ Patron has reached maximum borrowed books limit ({patron.MaxBooks})!");

                // Create loan
                var loan = new Loan(bookId, patronId, borrowFee);

                string query = @"
                    INSERT INTO loans (book_id, patron_id, borrowed_date, due_date, borrow_fee)
                    VALUES (@book_id, @patron_id, @borrowed, @due, @fee)";

                var parameters = new[]
                {
                    new NpgsqlParameter("@book_id", loan.BookId),
                    new NpgsqlParameter("@patron_id", loan.PatronId),
                    new NpgsqlParameter("@borrowed", loan.BorrowedDate),
                    new NpgsqlParameter("@due", loan.DueDate),
                    new NpgsqlParameter("@fee", loan.BorrowFee)
                };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);

                if (result > 0)
                {
                    // Update book availability
                    await _bookService.UpdateAvailabilityAsync(bookId, false);
                    return (true, $"✅ Book borrowed successfully!\n   Due date: {loan.DueDate:yyyy-MM-dd}\n   Fee: ₦{borrowFee:N2}");
                }

                return (false, "❌ Failed to create loan!");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Return a book
        /// </summary>
        public async Task<(bool success, string message, decimal fine)> ReturnBookAsync(int loanId)
        {
            try
            {
                // Get loan details
                var loan = await GetLoanByIdAsync(loanId);
                if (loan == null)
                    return (false, "❌ Loan not found!", 0);

                if (loan.IsReturned)
                    return (false, "❌ Book already returned!", 0);

                // Calculate overdue fine
                decimal fine = loan.CalculateOverdueFine(50m); // ₦50 per day

                // Update loan
                string query = @"
                    UPDATE loans 
                    SET is_returned = true, 
                        return_date = @return_date, 
                        overdue_fine = @fine 
                    WHERE id = @id";

                var parameters = new[]
                {
                    new NpgsqlParameter("@return_date", DateTime.Now),
                    new NpgsqlParameter("@fine", fine),
                    new NpgsqlParameter("@id", loanId)
                };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);

                if (result > 0)
                {
                    // Update book availability
                    await _bookService.UpdateAvailabilityAsync(loan.BookId, true);

                    string message = "✅ Book returned successfully!";
                    if (fine > 0)
                        message += $"\n   ⚠️ Overdue fine: ₦{fine:N2} ({loan.DaysOverdue()} days late)";
                    
                    return (true, message, fine);
                }

                return (false, "❌ Failed to return book!", 0);
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Get all active loans
        /// </summary>
        public async Task<List<Loan>> GetActiveLoansAsync()
        {
            var loans = new List<Loan>();

            try
            {
                string query = @"
                    SELECT l.*, b.title as book_title, p.name as patron_name
                    FROM loans l
                    JOIN books b ON l.book_id = b.id
                    JOIN patrons p ON l.patron_id = p.id
                    WHERE l.is_returned = false
                    ORDER BY l.due_date";

                using var reader = await _db.ExecuteReaderAsync(query);

                while (await reader.ReadAsync())
                {
                    loans.Add(MapReaderToLoan(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching active loans: {ex.Message}");
            }

            return loans;
        }

        /// <summary>
        /// Get all loans (active and returned)
        /// </summary>
        public async Task<List<Loan>> GetAllLoansAsync()
        {
            var loans = new List<Loan>();

            try
            {
                string query = @"
                    SELECT l.*, b.title as book_title, p.name as patron_name
                    FROM loans l
                    JOIN books b ON l.book_id = b.id
                    JOIN patrons p ON l.patron_id = p.id
                    ORDER BY l.borrowed_date DESC";

                using var reader = await _db.ExecuteReaderAsync(query);

                while (await reader.ReadAsync())
                {
                    loans.Add(MapReaderToLoan(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching loans: {ex.Message}");
            }

            return loans;
        }

        /// <summary>
        /// Get loan by ID
        /// </summary>
        public async Task<Loan?> GetLoanByIdAsync(int id)
        {
            try
            {
                string query = @"
                    SELECT l.*, b.title as book_title, p.name as patron_name
                    FROM loans l
                    JOIN books b ON l.book_id = b.id
                    JOIN patrons p ON l.patron_id = p.id
                    WHERE l.id = @id";

                var parameters = new[] { new NpgsqlParameter("@id", id) };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);

                if (await reader.ReadAsync())
                {
                    return MapReaderToLoan(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching loan: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Get loans by patron ID
        /// </summary>
        public async Task<List<Loan>> GetLoansByPatronAsync(int patronId)
        {
            var loans = new List<Loan>();

            try
            {
                string query = @"
                    SELECT l.*, b.title as book_title, p.name as patron_name
                    FROM loans l
                    JOIN books b ON l.book_id = b.id
                    JOIN patrons p ON l.patron_id = p.id
                    WHERE l.patron_id = @patron_id
                    ORDER BY l.borrowed_date DESC";

                var parameters = new[] { new NpgsqlParameter("@patron_id", patronId) };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);

                while (await reader.ReadAsync())
                {
                    loans.Add(MapReaderToLoan(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching patron loans: {ex.Message}");
            }

            return loans;
        }

        /// <summary>
        /// Get overdue loans
        /// </summary>
        public async Task<List<Loan>> GetOverdueLoansAsync()
        {
            var loans = new List<Loan>();

            try
            {
                string query = @"
                    SELECT l.*, b.title as book_title, p.name as patron_name
                    FROM loans l
                    JOIN books b ON l.book_id = b.id
                    JOIN patrons p ON l.patron_id = p.id
                    WHERE l.is_returned = false AND l.due_date < @today
                    ORDER BY l.due_date";

                var parameters = new[] { new NpgsqlParameter("@today", DateTime.Now.Date) };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);

                while (await reader.ReadAsync())
                {
                    loans.Add(MapReaderToLoan(reader));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching overdue loans: {ex.Message}");
            }

            return loans;
        }

        /// <summary>
        /// Map database reader to Loan object
        /// </summary>
        private Loan MapReaderToLoan(NpgsqlDataReader reader)
        {
            return new Loan
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                BookId = reader.GetInt32(reader.GetOrdinal("book_id")),
                PatronId = reader.GetInt32(reader.GetOrdinal("patron_id")),
                BorrowedDate = reader.GetDateTime(reader.GetOrdinal("borrowed_date")),
                DueDate = reader.GetDateTime(reader.GetOrdinal("due_date")),
                ReturnDate = reader.IsDBNull(reader.GetOrdinal("return_date")) ? null : reader.GetDateTime(reader.GetOrdinal("return_date")),
                IsReturned = reader.GetBoolean(reader.GetOrdinal("is_returned")),
                BorrowFee = reader.GetDecimal(reader.GetOrdinal("borrow_fee")),
                OverdueFine = reader.GetDecimal(reader.GetOrdinal("overdue_fine")),
                BookTitle = reader.GetString(reader.GetOrdinal("book_title")),
                PatronName = reader.GetString(reader.GetOrdinal("patron_name"))
            };
        }
    }
}
