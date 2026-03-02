using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LibraryOS.Models;

namespace LibraryOS.Services
{
    /// <summary>
    /// Service for searching books on the web using Open Library API
    /// FIX: Random instance is class-level to prevent identical ISBNs on rapid calls
    /// </summary>
    public class WebSearchService
    {
        private readonly HttpClient _httpClient;
        // FIX: single Random instance at class level — not inside the method
        private readonly Random _random = new Random();

        private const string OPEN_LIBRARY_SEARCH_URL = "https://openlibrary.org/search.json";
        private const string OPEN_LIBRARY_BOOK_URL   = "https://openlibrary.org";

        public WebSearchService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LibraryOS/1.1");
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<List<WebBookResult>> SearchBooksOnlineAsync(string query, int limit = 10)
        {
            var results = new List<WebBookResult>();
            try
            {
                var searchUrl = $"{OPEN_LIBRARY_SEARCH_URL}?q={Uri.EscapeDataString(query)}&limit={limit}";
                Console.WriteLine($"🌐 Searching Open Library for '{query}'...");

                var response = await _httpClient.GetStringAsync(searchUrl);

                using var document = JsonDocument.Parse(response);
                var root = document.RootElement;

                if (!root.TryGetProperty("docs", out JsonElement docs))
                {
                    Console.WriteLine("⚠️  No results from Open Library.");
                    return results;
                }

                foreach (var doc in docs.EnumerateArray())
                {
                    var bookResult = new WebBookResult();

                    bookResult.Title = doc.TryGetProperty("title", out var t)
                        ? t.GetString() ?? "Unknown Title" : "Unknown Title";

                    bookResult.Author = doc.TryGetProperty("author_name", out var a)
                        ? a.EnumerateArray().Select(x => x.GetString()).FirstOrDefault() ?? "Unknown Author"
                        : "Unknown Author";

                    bookResult.ISBN = doc.TryGetProperty("isbn", out var isbns)
                        ? isbns.EnumerateArray().Select(x => x.GetString()).FirstOrDefault() ?? GenerateRandomISBN()
                        : GenerateRandomISBN();

                    if (doc.TryGetProperty("first_publish_year", out var yr))
                        bookResult.PublicationYear = yr.GetInt32();

                    if (doc.TryGetProperty("publisher", out var pub))
                        bookResult.Publisher = pub.EnumerateArray().Select(x => x.GetString()).FirstOrDefault() ?? "";

                    if (doc.TryGetProperty("number_of_pages_median", out var pg))
                        bookResult.Pages = pg.GetInt32();

                    if (doc.TryGetProperty("subject", out var subj))
                        bookResult.Category = subj.EnumerateArray().Select(x => x.GetString()).FirstOrDefault() ?? "General";

                    if (doc.TryGetProperty("key", out var key))
                        bookResult.BookLink = $"{OPEN_LIBRARY_BOOK_URL}{key.GetString()}";

                    if (doc.TryGetProperty("cover_i", out var cov))
                        bookResult.CoverImageUrl = $"https://covers.openlibrary.org/b/id/{cov.GetInt32()}-M.jpg";

                    bookResult.Description = "Imported via Open Library. Edit description after adding.";
                    results.Add(bookResult);
                }

                Console.WriteLine($"✅ Found {results.Count} books online.");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Network error: {ex.Message}");
                Console.WriteLine("💡 Check your internet connection and try again.");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("❌ Request timed out. Open Library may be slow — try again.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error searching online: {ex.Message}");
            }
            return results;
        }

        public async Task<List<WebBookResult>> SearchByGenreAsync(string genre, int limit = 10)
            => await SearchBooksOnlineAsync($"subject:{genre}", limit);

        public async Task<List<WebBookResult>> SearchByAuthorAsync(string author, int limit = 10)
            => await SearchBooksOnlineAsync($"author:{author}", limit);

        public Book ConvertToBook(WebBookResult webResult) => new Book
        {
            Title           = webResult.Title,
            Author          = webResult.Author,
            ISBN            = webResult.ISBN,
            Category        = webResult.Category,
            Description     = webResult.Description,
            Publisher       = webResult.Publisher,
            PublicationYear = webResult.PublicationYear,
            Pages           = webResult.Pages,
            BookLink        = webResult.BookLink,
            CoverImageUrl   = webResult.CoverImageUrl,
            IsAvailable     = true,
            UploadStatus    = "approved"
        };

        // FIX: uses the class-level _random — safe for rapid bulk calls
        private string GenerateRandomISBN()
            => "978" + _random.Next(1_000_000_000, 2_000_000_000).ToString();

        public List<string> GetPopularGenres() => new()
        {
            "Fiction","Science Fiction","Fantasy","Mystery","Romance","Thriller",
            "Biography","History","Science","Philosophy","Psychology","Self-Help",
            "Business","Programming","Technology","Poetry","Drama","Adventure","Horror","Comedy"
        };
    }

    public class WebBookResult
    {
        public string Title           { get; set; } = string.Empty;
        public string Author          { get; set; } = string.Empty;
        public string ISBN            { get; set; } = string.Empty;
        public string Category        { get; set; } = string.Empty;
        public string Description     { get; set; } = string.Empty;
        public string Publisher       { get; set; } = string.Empty;
        public int    PublicationYear { get; set; }
        public int    Pages           { get; set; }
        public string BookLink        { get; set; } = string.Empty;
        public string CoverImageUrl   { get; set; } = string.Empty;

        public override string ToString() =>
            $"📖 {Title}\n" +
            $"   ✍️  {Author}\n" +
            $"   📅 {PublicationYear}\n" +
            $"   📚 {Category}\n" +
            $"   📄 {Pages} pages\n" +
            $"   🔢 ISBN: {ISBN}";
    }
}
