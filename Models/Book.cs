namespace LibraryOS.Models
{
    /// <summary>
    /// Represents a book in the library system
    /// </summary>
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public string? Category { get; set; }
        public string? BookLink { get; set; }
        public bool IsAvailable { get; set; }
        public int? UploadedBy { get; set; }
        public string UploadStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Description { get; set; }
        public string? Publisher { get; set; }
        public int? PublicationYear { get; set; }
        public int? Pages { get; set; }
        public string? Language { get; set; }
        public string? CoverImageUrl { get; set; }

        public Book()
        {
            Title = string.Empty;
            Author = string.Empty;
            ISBN = string.Empty;
            IsAvailable = true;
            UploadStatus = "approved";
            CreatedAt = DateTime.Now;
        }

        public Book(string title, string author, string isbn)
        {
            Title = title;
            Author = author;
            ISBN = isbn;
            IsAvailable = true;
            UploadStatus = "approved";
            CreatedAt = DateTime.Now;
        }

        public override string ToString()
        {
            string status = IsAvailable ? "📗 Available" : "📕 Borrowed";
            return $"[{Id}] {Title} by {Author} - {status}";
        }

        public string GetDetailedInfo()
        {
            return $@"
═══════════════════════════════════════
📚 Book Details
═══════════════════════════════════════
ID:       {Id}
Title:    {Title}
Author:   {Author}
ISBN:     {ISBN}
Category: {Category ?? "N/A"}
Status:   {(IsAvailable ? "✅ Available" : "❌ Borrowed")}
Link:     {BookLink ?? "N/A"}
═══════════════════════════════════════";
        }
    }
}
