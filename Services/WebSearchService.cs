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
    /// </summary>
    public class WebSearchService
    {
        private readonly HttpClient _httpClient;
        private const string OPEN_LIBRARY_SEARCH_URL = "https://openlibrary.org/search.json";
        private const string OPEN_LIBRARY_BOOK_URL = "https://openlibrary.org";

        public WebSearchService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "LibraryOS/1.0");
        }

        /// <summary>
        /// Search for books on Open Library
        /// </summary>
        public async Task<List<WebBookResult>> SearchBooksOnlineAsync(string query, int limit = 10)
        {
            var results = new List<WebBookResult>();

            try
            {
                // Build the search URL
                var searchUrl = $"{OPEN_LIBRARY_SEARCH_URL}?q={Uri.EscapeDataString(query)}&limit={limit}";
                
                Console.WriteLine($"🔍 Searching for '{query}' online...");
                
                // Make the API request
                var response = await _httpClient.GetStringAsync(searchUrl);
                
                // Parse the JSON response
                using (JsonDocument document = JsonDocument.Parse(response))
                {
                    var root = document.RootElement;
                    
                    if (root.TryGetProperty("docs", out JsonElement docs))
                    {
                        foreach (var doc in docs.EnumerateArray())
                        {
                            var bookResult = new WebBookResult();

                            // Extract title
                            if (doc.TryGetProperty("title", out JsonElement title))
                            {
                                bookResult.Title = title.GetString() ?? "Unknown Title";
                            }

                            // Extract author
                            if (doc.TryGetProperty("author_name", out JsonElement authorName))
                            {
                                var authors = authorName.EnumerateArray().Select(a => a.GetString()).ToList();
                                bookResult.Author = authors.FirstOrDefault() ?? "Unknown Author";
                            }

                            // Extract ISBN
                            if (doc.TryGetProperty("isbn", out JsonElement isbns))
                            {
                                var isbnList = isbns.EnumerateArray().Select(i => i.GetString()).ToList();
                                bookResult.ISBN = isbnList.FirstOrDefault() ?? GenerateRandomISBN();
                            }
                            else
                            {
                                bookResult.ISBN = GenerateRandomISBN();
                            }

                            // Extract first publish year
                            if (doc.TryGetProperty("first_publish_year", out JsonElement year))
                            {
                                bookResult.PublicationYear = year.GetInt32();
                            }

                            // Extract publisher
                            if (doc.TryGetProperty("publisher", out JsonElement publishers))
                            {
                                var publisherList = publishers.EnumerateArray().Select(p => p.GetString()).ToList();
                                bookResult.Publisher = publisherList.FirstOrDefault() ?? "Unknown Publisher";
                            }

                            // Extract number of pages
                            if (doc.TryGetProperty("number_of_pages_median", out JsonElement pages))
                            {
                                bookResult.Pages = pages.GetInt32();
                            }

                            // Extract subject/category
                            if (doc.TryGetProperty("subject", out JsonElement subjects))
                            {
                                var subjectList = subjects.EnumerateArray().Select(s => s.GetString()).ToList();
                                bookResult.Category = subjectList.FirstOrDefault() ?? "General";
                            }

                            // Build book link
                            if (doc.TryGetProperty("key", out JsonElement key))
                            {
                                bookResult.BookLink = $"{OPEN_LIBRARY_BOOK_URL}{key.GetString()}";
                            }

                            // Extract cover image
                            if (doc.TryGetProperty("cover_i", out JsonElement coverId))
                            {
                                bookResult.CoverImageUrl = $"https://covers.openlibrary.org/b/id/{coverId.GetInt32()}-M.jpg";
                            }

                            // Add description (usually not in search results, but we can note it)
                            bookResult.Description = "Book found via web search. Add custom description after import.";

                            results.Add(bookResult);
                        }
                    }
                }

                Console.WriteLine($"✅ Found {results.Count} books online!");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Network error: {ex.Message}");
                Console.WriteLine("💡 Make sure you have an internet connection.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error searching online: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Search by genre/category
        /// </summary>
        public async Task<List<WebBookResult>> SearchByGenreAsync(string genre, int limit = 10)
        {
            return await SearchBooksOnlineAsync($"subject:{genre}", limit);
        }

        /// <summary>
        /// Search by author
        /// </summary>
        public async Task<List<WebBookResult>> SearchByAuthorAsync(string author, int limit = 10)
        {
            return await SearchBooksOnlineAsync($"author:{author}", limit);
        }

        /// <summary>
        /// Convert WebBookResult to Book model for database insertion
        /// </summary>
        public Book ConvertToBook(WebBookResult webResult)
        {
            return new Book
            {
                Title = webResult.Title,
                Author = webResult.Author,
                ISBN = webResult.ISBN,
                Category = webResult.Category,
                Description = webResult.Description,
                Publisher = webResult.Publisher,
                PublicationYear = webResult.PublicationYear,
                Pages = webResult.Pages,
                BookLink = webResult.BookLink,
                CoverImageUrl = webResult.CoverImageUrl,
                IsAvailable = true,
                UploadStatus = "approved"
            };
        }

        /// <summary>
        /// Generate a random ISBN for books without one
        /// </summary>
        private string GenerateRandomISBN()
        {
            var random = new Random();
            var isbn = "978" + random.Next(1000000000, 2000000000).ToString();
            return isbn;
        }

        /// <summary>
        /// Get popular genres for browsing
        /// </summary>
        public List<string> GetPopularGenres()
        {
            return new List<string>
            {
                "Fiction",
                "Science Fiction",
                "Fantasy",
                "Mystery",
                "Romance",
                "Thriller",
                "Biography",
                "History",
                "Science",
                "Philosophy",
                "Psychology",
                "Self-Help",
                "Business",
                "Programming",
                "Technology",
                "Poetry",
                "Drama",
                "Adventure",
                "Horror",
                "Comedy"
            };
        }
    }

    /// <summary>
    /// Model for web search results
    /// </summary>
    public class WebBookResult
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public int Pages { get; set; }
        public string BookLink { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"📖 {Title}\n" +
                   $"   ✍️  {Author}\n" +
                   $"   📅 {PublicationYear}\n" +
                   $"   📚 {Category}\n" +
                   $"   📄 {Pages} pages\n" +
                   $"   🔢 ISBN: {ISBN}";
        }
    }
}