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
        /// Borrow a book — now supports custom loan period (days)
        /// </summary>
        public async Task<(bool success, string message)> BorrowBookAsync(
            int bookId, int patronId, decimal borrowFee = 100m, int loanDays = 14)
        {
            try
            {
                var book = await _bookService.GetBookByIdAsync(bookId);
                if (book == null)
                    return (false, "❌ Book not found! Check Book ID using 'View All Books'.");

                if (!book.IsAvailable)
                    return (false, "❌ This book is currently borrowed by someone else.");

                var patron = await _patronService.GetPatronByIdAsync(patronId);
                if (patron == null)
                    return (false, "❌ Patron not found! Check Patron ID using 'View All Patrons'.");

                if (!patron.IsActive)
                    return (false, "❌ This patron's account is inactive.");

                bool canBorrow = await _patronService.CanBorrowAsync(patronId);
                if (!canBorrow)
                    return (false, $"❌ Patron has reached the maximum of {patron.MaxBooks} borrowed books. Return one first.");

                var loan = new Loan(bookId, patronId, borrowFee)
                {
                    DueDate = DateTime.Now.AddDays(loanDays)
                };

                string query = @"
                    INSERT INTO loans (book_id, patron_id, borrowed_date, due_date, borrow_fee)
                    VALUES (@book_id, @patron_id, @borrowed, @due, @fee)
                    RETURNING id";

                var parameters = new[]
                {
                    new NpgsqlParameter("@book_id",   loan.BookId),
                    new NpgsqlParameter("@patron_id", loan.PatronId),
                    new NpgsqlParameter("@borrowed",  loan.BorrowedDate.Date),
                    new NpgsqlParameter("@due",       loan.DueDate.Date),
                    new NpgsqlParameter("@fee",       loan.BorrowFee)
                };

                var result = await _db.ExecuteScalarAsync(query, parameters);
                if (result != null)
                {
                    await _bookService.UpdateAvailabilityAsync(bookId, false);
                    return (true,
                        $"✅ Book borrowed successfully!\n" +
                        $"   Loan ID:   {result}\n" +
                        $"   Book:      {book.Title}\n" +
                        $"   Patron:    {patron.Name}\n" +
                        $"   Due date:  {loan.DueDate:yyyy-MM-dd}\n" +
                        $"   Fee:       ₦{borrowFee:N2}");
                }

                return (false, "❌ Failed to create loan record.");
            }
            catch (Exception ex)
            {
                return (false, $"❌ Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Return a book — FIX: stores only Date portion to match loans.return_date DATE column
        /// </summary>
        public async Task<(bool success, string message, decimal fine)> ReturnBookAsync(int loanId)
        {
            try
            {
                var loan = await GetLoanByIdAsync(loanId);
                if (loan == null)
                    return (false, "❌ Loan not found! Use 'View Active Loans' to get valid IDs.", 0);

                if (loan.IsReturned)
                    return (false, "❌ This book was already returned.", 0);

                decimal fine = loan.CalculateOverdueFine(50m); // ₦50/day

                string query = @"
                    UPDATE loans 
                    SET is_returned = true, 
                        return_date  = @return_date, 
                        overdue_fine = @fine 
                    WHERE id = @id";

                var parameters = new[]
                {
                    // FIX: .Date strips the time component — avoids timezone edge-cases with DATE column
                    new NpgsqlParameter("@return_date", DateTime.Now.Date),
                    new NpgsqlParameter("@fine",        fine),
                    new NpgsqlParameter("@id",          loanId)
                };

                int result = await _db.ExecuteNonQueryAsync(query, parameters);
                if (result > 0)
                {
                    await _bookService.UpdateAvailabilityAsync(loan.BookId, true);
                    string message = $"✅ Book returned successfully!\n   Book: {loan.BookTitle}";
                    if (fine > 0)
                        message += $"\n   ⚠️  Overdue fine: ₦{fine:N2} ({loan.DaysOverdue()} days late)";
                    return (true, message, fine);
                }

                return (false, "❌ Failed to record return.", 0);
            }
            catch (Exception ex)
            {
                return (false, $"❌ Error: {ex.Message}", 0);
            }
        }

        public async Task<List<Loan>> GetActiveLoansAsync()
        {
            var loans = new List<Loan>();
            try
            {
                string query = @"
                    SELECT l.*, b.title AS book_title, p.name AS patron_name
                    FROM loans l
                    JOIN books   b ON l.book_id   = b.id
                    JOIN patrons p ON l.patron_id  = p.id
                    WHERE l.is_returned = false
                    ORDER BY l.due_date";

                using var reader = await _db.ExecuteReaderAsync(query);
                while (await reader.ReadAsync())
                    loans.Add(MapReaderToLoan(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching active loans: {ex.Message}");
            }
            return loans;
        }

        public async Task<List<Loan>> GetAllLoansAsync()
        {
            var loans = new List<Loan>();
            try
            {
                string query = @"
                    SELECT l.*, b.title AS book_title, p.name AS patron_name
                    FROM loans l
                    JOIN books   b ON l.book_id   = b.id
                    JOIN patrons p ON l.patron_id  = p.id
                    ORDER BY l.borrowed_date DESC";

                using var reader = await _db.ExecuteReaderAsync(query);
                while (await reader.ReadAsync())
                    loans.Add(MapReaderToLoan(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching loans: {ex.Message}");
            }
            return loans;
        }

        public async Task<Loan?> GetLoanByIdAsync(int id)
        {
            try
            {
                string query = @"
                    SELECT l.*, b.title AS book_title, p.name AS patron_name
                    FROM loans l
                    JOIN books   b ON l.book_id   = b.id
                    JOIN patrons p ON l.patron_id  = p.id
                    WHERE l.id = @id";

                var parameters = new[] { new NpgsqlParameter("@id", id) };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);
                if (await reader.ReadAsync())
                    return MapReaderToLoan(reader);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching loan: {ex.Message}");
            }
            return null;
        }

        public async Task<List<Loan>> GetLoansByPatronAsync(int patronId)
        {
            var loans = new List<Loan>();
            try
            {
                string query = @"
                    SELECT l.*, b.title AS book_title, p.name AS patron_name
                    FROM loans l
                    JOIN books   b ON l.book_id   = b.id
                    JOIN patrons p ON l.patron_id  = p.id
                    WHERE l.patron_id = @patron_id
                    ORDER BY l.borrowed_date DESC";

                var parameters = new[] { new NpgsqlParameter("@patron_id", patronId) };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);
                while (await reader.ReadAsync())
                    loans.Add(MapReaderToLoan(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching patron loans: {ex.Message}");
            }
            return loans;
        }

        public async Task<List<Loan>> GetOverdueLoansAsync()
        {
            var loans = new List<Loan>();
            try
            {
                string query = @"
                    SELECT l.*, b.title AS book_title, p.name AS patron_name
                    FROM loans l
                    JOIN books   b ON l.book_id   = b.id
                    JOIN patrons p ON l.patron_id  = p.id
                    WHERE l.is_returned = false AND l.due_date < @today
                    ORDER BY l.due_date";

                var parameters = new[] { new NpgsqlParameter("@today", DateTime.Now.Date) };
                using var reader = await _db.ExecuteReaderAsync(query, parameters);
                while (await reader.ReadAsync())
                    loans.Add(MapReaderToLoan(reader));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching overdue loans: {ex.Message}");
            }
            return loans;
        }

        private Loan MapReaderToLoan(NpgsqlDataReader reader)
        {
            return new Loan
            {
                Id           = reader.GetInt32(reader.GetOrdinal("id")),
                BookId       = reader.GetInt32(reader.GetOrdinal("book_id")),
                PatronId     = reader.GetInt32(reader.GetOrdinal("patron_id")),
                BorrowedDate = reader.GetDateTime(reader.GetOrdinal("borrowed_date")),
                DueDate      = reader.GetDateTime(reader.GetOrdinal("due_date")),
                ReturnDate   = reader.IsDBNull(reader.GetOrdinal("return_date")) ? null : reader.GetDateTime(reader.GetOrdinal("return_date")),
                IsReturned   = reader.GetBoolean(reader.GetOrdinal("is_returned")),
                BorrowFee    = reader.GetDecimal(reader.GetOrdinal("borrow_fee")),
                OverdueFine  = reader.GetDecimal(reader.GetOrdinal("overdue_fine")),
                BookTitle    = reader.GetString(reader.GetOrdinal("book_title")),
                PatronName   = reader.GetString(reader.GetOrdinal("patron_name")),
            };
        }
    }
}
