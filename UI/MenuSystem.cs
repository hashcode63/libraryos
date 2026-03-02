using LibraryOS.Models;
using LibraryOS.Services;

namespace LibraryOS.UI
{
    /// <summary>
    /// Handles all menu interactions and user flows
    /// </summary>
    public class MenuSystem
    {
        private readonly BookService _bookService;
        private readonly PatronService _patronService;
        private readonly LoanService _loanService;
        private readonly WebSearchService _webSearchService;

        public MenuSystem(BookService bookService, PatronService patronService, LoanService loanService, WebSearchService webSearchService)
        {
            _bookService = bookService;
            _patronService = patronService;
            _loanService = loanService;
            _webSearchService = webSearchService;
        }

        public async Task ShowMainMenuAsync()
        {
            while (true)
            {
                ConsoleUI.Clear();
                ConsoleUI.ShowLogo();
                ConsoleUI.ShowMenu("Main Menu",
                    "📚 Book Management",
                    "👤 Patron Management",
                    "🔄 Loans (Borrow/Return)",
                    "🔍 Search & Reports",
                    "🚪 Exit");

                int choice = ConsoleUI.GetIntInput("\nSelect option ➜");

                switch (choice)
                {
                    case 1:
                        await ShowBookMenuAsync();
                        break;
                    case 2:
                        await ShowPatronMenuAsync();
                        break;
                    case 3:
                        await ShowLoanMenuAsync();
                        break;
                    case 4:
                        await ShowSearchMenuAsync();
                        break;
                    case 5:
                    case 0:
                        ConsoleUI.ShowInfo("\n👋 Thank you for using LibraryOS!");
                        return;
                    default:
                        ConsoleUI.ShowError("❌ Invalid option!");
                        ConsoleUI.WaitForKey();
                        break;
                }
            }
        }

        private async Task ShowBookMenuAsync()
        {
            while (true)
            {
                ConsoleUI.Clear();
                ConsoleUI.ShowMenu("Book Management",
                    "Add Book",
                    "Remove Book",
                    "View All Books",
                    "Check Availability",
                    "Upload Book (User)");

                int choice = ConsoleUI.GetIntInput("\nChoose ➜");

                switch (choice)
                {
                    case 1:
                        await AddBookAsync();
                        break;
                    case 2:
                        await RemoveBookAsync();
                        break;
                    case 3:
                        await ViewAllBooksAsync();
                        break;
                    case 4:
                        await CheckAvailabilityAsync();
                        break;
                    case 5:
                        await UploadBookAsync();
                        break;
                    case 0:
                        return;
                    default:
                        ConsoleUI.ShowError("❌ Invalid option!");
                        ConsoleUI.WaitForKey();
                        break;
                }
            }
        }

        private async Task ShowPatronMenuAsync()
        {
            while (true)
            {
                ConsoleUI.Clear();
                ConsoleUI.ShowMenu("Patron Management",
                    "Register Patron",
                    "Remove Patron",
                    "View All Patrons",
                    "View Patron Details");

                int choice = ConsoleUI.GetIntInput("\nChoose ➜");

                switch (choice)
                {
                    case 1:
                        await RegisterPatronAsync();
                        break;
                    case 2:
                        await RemovePatronAsync();
                        break;
                    case 3:
                        await ViewAllPatronsAsync();
                        break;
                    case 4:
                        await ViewPatronDetailsAsync();
                        break;
                    case 0:
                        return;
                    default:
                        ConsoleUI.ShowError("❌ Invalid option!");
                        ConsoleUI.WaitForKey();
                        break;
                }
            }
        }

        private async Task ShowLoanMenuAsync()
        {
            while (true)
            {
                ConsoleUI.Clear();
                ConsoleUI.ShowMenu("Loan System",
                    "Borrow Book",
                    "Return Book",
                    "View Active Loans",
                    "View Overdue Loans");

                int choice = ConsoleUI.GetIntInput("\nChoose ➜");

                switch (choice)
                {
                    case 1:
                        await BorrowBookAsync();
                        break;
                    case 2:
                        await ReturnBookAsync();
                        break;
                    case 3:
                        await ViewActiveLoansAsync();
                        break;
                    case 4:
                        await ViewOverdueLoansAsync();
                        break;
                    case 0:
                        return;
                    default:
                        ConsoleUI.ShowError("❌ Invalid option!");
                        ConsoleUI.WaitForKey();
                        break;
                }
            }
        }

        private async Task ShowSearchMenuAsync()
        {
            while (true)
            {
                ConsoleUI.Clear();
                ConsoleUI.ShowMenu("Search & Reports",
                    "Search Books",
                    "View Reports");

                int choice = ConsoleUI.GetIntInput("\nChoose ➜");

                switch (choice)
                {
                    case 1:
                        await SearchBooksAsync();
                        break;
                    case 2:
                        await ShowReportsAsync();
                        break;
                    case 0:
                        return;
                    default:
                        ConsoleUI.ShowError("❌ Invalid option!");
                        ConsoleUI.WaitForKey();
                        break;
                }
            }
        }

        // Book Operations
        private async Task AddBookAsync()
        {
            ConsoleUI.ShowHeader("Add New Book");

            string title = ConsoleUI.GetInput("📖 Enter Title:");
            string author = ConsoleUI.GetInput("✍️ Enter Author:");
            string isbn = ConsoleUI.GetInput("🔢 Enter ISBN:");
            string category = ConsoleUI.GetInput("📂 Enter Category (optional):");

            var book = new Book(title, author, isbn)
            {
                Category = string.IsNullOrWhiteSpace(category) ? null : category
            };

            ConsoleUI.ShowLoading("Adding book");
            bool success = await _bookService.AddBookAsync(book);

            if (success)
                ConsoleUI.ShowSuccess("✅ Book added successfully!");
            else
                ConsoleUI.ShowError("❌ Failed to add book!");

            ConsoleUI.WaitForKey();
        }

        private async Task UploadBookAsync()
        {
            ConsoleUI.ShowHeader("Upload Book");

            string title = ConsoleUI.GetInput("📖 Enter Title:");
            string author = ConsoleUI.GetInput("✍️ Enter Author:");
            string isbn = ConsoleUI.GetInput("🔢 Enter ISBN:");
            string category = ConsoleUI.GetInput("📂 Enter Category:");
            string bookLink = ConsoleUI.GetInput("🔗 Enter Book Link (URL):");
            int patronId = ConsoleUI.GetIntInput("👤 Enter Your Patron ID:");

            var book = new Book(title, author, isbn)
            {
                Category = category,
                BookLink = bookLink,
                UploadedBy = patronId,
                UploadStatus = "pending"
            };

            ConsoleUI.ShowLoading("Uploading book");
            bool success = await _bookService.AddBookAsync(book);

            if (success)
                ConsoleUI.ShowSuccess("✅ Book uploaded! Waiting for admin approval.");
            else
                ConsoleUI.ShowError("❌ Upload failed!");

            ConsoleUI.WaitForKey();
        }

        private async Task RemoveBookAsync()
        {
            ConsoleUI.ShowHeader("Remove Book");

            int bookId = ConsoleUI.GetIntInput("Enter Book ID:");

            bool confirm = ConsoleUI.GetYesNoInput("⚠️ Are you sure you want to remove this book?");
            if (!confirm)
            {
                ConsoleUI.ShowInfo("Cancelled.");
                ConsoleUI.WaitForKey();
                return;
            }

            ConsoleUI.ShowLoading("Removing book");
            bool success = await _bookService.RemoveBookAsync(bookId);

            if (success)
                ConsoleUI.ShowSuccess("✅ Book removed successfully!");
            else
                ConsoleUI.ShowError("❌ Failed to remove book!");

            ConsoleUI.WaitForKey();
        }

        private async Task ViewAllBooksAsync()
        {
            ConsoleUI.ShowHeader("All Books");
            ConsoleUI.ShowLoading("Loading books");

            var books = await _bookService.GetAllBooksAsync();

            if (books.Count == 0)
            {
                ConsoleUI.ShowWarning("No books found.");
            }
            else
            {
                Console.WriteLine();
                foreach (var book in books)
                {
                    ConsoleUI.ShowInfo(book.ToString());
                }
                ConsoleUI.ShowDivider();
                ConsoleUI.ShowHighlight($"Total: {books.Count} books");
            }

            ConsoleUI.WaitForKey();
        }

        private async Task CheckAvailabilityAsync()
        {
            ConsoleUI.ShowHeader("Check Availability");

            int bookId = ConsoleUI.GetIntInput("Enter Book ID:");

            var book = await _bookService.GetBookByIdAsync(bookId);

            if (book == null)
            {
                ConsoleUI.ShowError("❌ Book not found!");
            }
            else
            {
                Console.WriteLine(book.GetDetailedInfo());
            }

            ConsoleUI.WaitForKey();
        }

        // Patron Operations
        private async Task RegisterPatronAsync()
        {
            ConsoleUI.ShowHeader("Register Patron");

            string name = ConsoleUI.GetInput("👤 Enter Name:");
            string email = ConsoleUI.GetInput("📧 Enter Email:");
            string phone = ConsoleUI.GetInput("📱 Enter Phone (optional):");

            var patron = new Patron(name, email, string.IsNullOrWhiteSpace(phone) ? null : phone);

            ConsoleUI.ShowLoading("Registering patron");
            bool success = await _patronService.RegisterPatronAsync(patron);

            if (success)
                ConsoleUI.ShowSuccess("✅ Patron registered successfully!");
            else
                ConsoleUI.ShowError("❌ Failed to register patron!");

            ConsoleUI.WaitForKey();
        }

        private async Task RemovePatronAsync()
        {
            ConsoleUI.ShowHeader("Remove Patron");

            int patronId = ConsoleUI.GetIntInput("Enter Patron ID:");

            bool confirm = ConsoleUI.GetYesNoInput("⚠️ Are you sure?");
            if (!confirm)
            {
                ConsoleUI.ShowInfo("Cancelled.");
                ConsoleUI.WaitForKey();
                return;
            }

            ConsoleUI.ShowLoading("Removing patron");
            bool success = await _patronService.RemovePatronAsync(patronId);

            if (success)
                ConsoleUI.ShowSuccess("✅ Patron removed!");
            else
                ConsoleUI.ShowError("❌ Failed to remove patron!");

            ConsoleUI.WaitForKey();
        }

        private async Task ViewAllPatronsAsync()
        {
            ConsoleUI.ShowHeader("All Patrons");
            ConsoleUI.ShowLoading("Loading patrons");

            var patrons = await _patronService.GetAllPatronsAsync();

            if (patrons.Count == 0)
            {
                ConsoleUI.ShowWarning("No patrons found.");
            }
            else
            {
                Console.WriteLine();
                foreach (var patron in patrons)
                {
                    ConsoleUI.ShowInfo(patron.ToString());
                }
                ConsoleUI.ShowDivider();
                ConsoleUI.ShowHighlight($"Total: {patrons.Count} patrons");
            }

            ConsoleUI.WaitForKey();
        }

        private async Task ViewPatronDetailsAsync()
        {
            ConsoleUI.ShowHeader("Patron Details");

            int patronId = ConsoleUI.GetIntInput("Enter Patron ID:");

            var patron = await _patronService.GetPatronByIdAsync(patronId);

            if (patron == null)
            {
                ConsoleUI.ShowError("❌ Patron not found!");
            }
            else
            {
                Console.WriteLine(patron.GetDetailedInfo());

                var loans = await _loanService.GetLoansByPatronAsync(patronId);
                int activeLoans = loans.Count(l => !l.IsReturned);

                ConsoleUI.ShowInfo($"\n📚 Currently borrowed: {activeLoans}/{patron.MaxBooks} books");
            }

            ConsoleUI.WaitForKey();
        }

        // Loan Operations
        private async Task BorrowBookAsync()
        {
            ConsoleUI.ShowHeader("Borrow Book");

            int bookId = ConsoleUI.GetIntInput("📚 Enter Book ID:");
            int patronId = ConsoleUI.GetIntInput("👤 Enter Patron ID:");
            decimal fee = ConsoleUI.GetDecimalInput("💰 Enter Borrow Fee (₦):");

            bool confirm = ConsoleUI.GetYesNoInput($"Confirm borrow?\nBook ID: {bookId}\nPatron ID: {patronId}\nFee: ₦{fee:N2}");

            if (!confirm)
            {
                ConsoleUI.ShowInfo("Cancelled.");
                ConsoleUI.WaitForKey();
                return;
            }

            ConsoleUI.ShowLoading("Processing");
            var (success, message) = await _loanService.BorrowBookAsync(bookId, patronId, fee);

            if (success)
                ConsoleUI.ShowSuccess(message);
            else
                ConsoleUI.ShowError(message);

            ConsoleUI.WaitForKey();
        }

        private async Task ReturnBookAsync()
        {
            ConsoleUI.ShowHeader("Return Book");

            int loanId = ConsoleUI.GetIntInput("🔄 Enter Loan ID:");

            ConsoleUI.ShowLoading("Processing return");
            var (success, message, fine) = await _loanService.ReturnBookAsync(loanId);

            if (success)
                ConsoleUI.ShowSuccess(message);
            else
                ConsoleUI.ShowError(message);

            ConsoleUI.WaitForKey();
        }

        private async Task ViewActiveLoansAsync()
        {
            ConsoleUI.ShowHeader("Active Loans");
            ConsoleUI.ShowLoading("Loading loans");

            var loans = await _loanService.GetActiveLoansAsync();

            if (loans.Count == 0)
            {
                ConsoleUI.ShowWarning("No active loans.");
            }
            else
            {
                Console.WriteLine();
                foreach (var loan in loans)
                {
                    ConsoleUI.ShowInfo(loan.ToString());
                }
                ConsoleUI.ShowDivider();
                ConsoleUI.ShowHighlight($"Total: {loans.Count} active loans");
            }

            ConsoleUI.WaitForKey();
        }

        private async Task ViewOverdueLoansAsync()
        {
            ConsoleUI.ShowHeader("Overdue Loans");
            ConsoleUI.ShowLoading("Loading overdue loans");

            var loans = await _loanService.GetOverdueLoansAsync();

            if (loans.Count == 0)
            {
                ConsoleUI.ShowSuccess("✅ No overdue loans!");
            }
            else
            {
                Console.WriteLine();
                foreach (var loan in loans)
                {
                    decimal fine = loan.CalculateOverdueFine(50m);
                    ConsoleUI.ShowWarning($"{loan} - Fine: ₦{fine:N2}");
                }
                ConsoleUI.ShowDivider();
                ConsoleUI.ShowHighlight($"Total: {loans.Count} overdue loans");
            }

            ConsoleUI.WaitForKey();
        }

        private async Task SearchBooksAsync()
        {
            ConsoleUI.ShowHeader("Search Books");

            string keyword = ConsoleUI.GetInput("🔍 Enter search term (title/author):");

            ConsoleUI.ShowLoading("Searching");
            var books = await _bookService.SearchBooksAsync(keyword);

            if (books.Count == 0)
            {
                ConsoleUI.ShowWarning($"No books found matching '{keyword}'");
            }
            else
            {
                Console.WriteLine();
                foreach (var book in books)
                {
                    ConsoleUI.ShowInfo(book.ToString());
                }
                ConsoleUI.ShowDivider();
                ConsoleUI.ShowHighlight($"Found: {books.Count} books");
            }

            ConsoleUI.WaitForKey();
        }

        private async Task ShowReportsAsync()
        {
            ConsoleUI.ShowHeader("Library Reports");
            ConsoleUI.ShowLoading("Generating reports");

            var allBooks = await _bookService.GetAllBooksAsync();
            int availableBooks = await _bookService.GetAvailableBooksCountAsync();
            int borrowedBooks = allBooks.Count - availableBooks;

            var activePatrons = await _patronService.GetActivePatronsCountAsync();
            var activeLoans = await _loanService.GetActiveLoansAsync();
            var overdueLoans = await _loanService.GetOverdueLoansAsync();

            Console.WriteLine();
            ConsoleUI.ShowInfo($"📚 Total Books:      {allBooks.Count}");
            ConsoleUI.ShowInfo($"📗 Available:        {availableBooks}");
            ConsoleUI.ShowInfo($"📕 Borrowed:         {borrowedBooks}");
            ConsoleUI.ShowDivider();
            ConsoleUI.ShowInfo($"👤 Active Patrons:   {activePatrons}");
            ConsoleUI.ShowInfo($"🔄 Active Loans:     {activeLoans.Count}");
            ConsoleUI.ShowInfo($"⚠️ Overdue Loans:    {overdueLoans.Count}");

            ConsoleUI.WaitForKey();
        }
    }
}
