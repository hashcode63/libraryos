using LibraryOS.Services;
using LibraryOS.UI;

namespace LibraryOS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // IMPORTANT: Replace with your actual Neon connection string
            // Get this from: https://neon.tech
           string connectionString = 
    "Host=ep-patient-wind-aikicjkg-pooler.c-4.us-east-1.aws.neon.tech;" +
    "Port=5432;" +
    "Database=neondb;" +
    "Username=neondb_owner;" +
    "Password=npg_4laQzhYv1WVc;" +
    "SSL Mode=Require;" +
    "Trust Server Certificate=true;";

            // Check if connection string is configured
            if (connectionString.Contains("YOUR_NEON_HOST"))
            {
                ConsoleUI.ShowError("❌ ERROR: Database not configured!");
                Console.WriteLine("\nPlease update the connection string in Program.cs");
                Console.WriteLine("\nSteps:");
                Console.WriteLine("1. Create a free database at https://neon.tech");
                Console.WriteLine("2. Copy your connection string");
                Console.WriteLine("3. Replace the connectionString variable in Program.cs");
                Console.WriteLine("4. Run the database_schema.sql to create tables");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            try
            {
                // Initialize services
                var dbService = new DatabaseService(connectionString);
                var bookService = new BookService(dbService);
                var patronService = new PatronService(dbService);
                var loanService = new LoanService(dbService, bookService, patronService);

                // Test database connection
                ConsoleUI.ShowLoading("Connecting to database");
                bool isConnected = await dbService.TestConnectionAsync();

                if (!isConnected)
                {
                    ConsoleUI.ShowError("❌ Database connection failed!");
                    Console.WriteLine("\nPlease check:");
                    Console.WriteLine("- Connection string is correct");
                    Console.WriteLine("- Database is running");
                    Console.WriteLine("- Network connection is available");
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey();
                    return;
                }

                ConsoleUI.ShowSuccess("✅ Connected to database!");

                // Initialize database tables (creates tables if they don't exist)
                await dbService.InitializeDatabaseAsync();

                // Small delay to show success message
                await Task.Delay(1000);

                // Start the menu system
                var webSearchService = new WebSearchService();
                var menuSystem = new MenuSystem(bookService, patronService, loanService, webSearchService);
                await menuSystem.ShowMainMenuAsync();
            }
            catch (Exception ex)
            {
                ConsoleUI.ShowError($"❌ Fatal Error: {ex.Message}");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
