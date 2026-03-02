namespace LibraryOS.Models
{
    /// <summary>
    /// Represents a book loan transaction
    /// </summary>
    public class Loan
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int PatronId { get; set; }
        public DateTime BorrowedDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public bool IsReturned { get; set; }
        public decimal BorrowFee { get; set; }
        public decimal OverdueFine { get; set; }

        // Navigation properties (for display purposes)
        public string? BookTitle { get; set; }
        public string? PatronName { get; set; }

        public Loan()
        {
            BorrowedDate = DateTime.Now;
            DueDate = DateTime.Now.AddDays(14); // 2 weeks default
            IsReturned = false;
            BorrowFee = 100m; // ₦100 default fee
            OverdueFine = 0m;
        }

        public Loan(int bookId, int patronId, decimal borrowFee = 100m)
        {
            BookId = bookId;
            PatronId = patronId;
            BorrowedDate = DateTime.Now;
            DueDate = DateTime.Now.AddDays(14);
            IsReturned = false;
            BorrowFee = borrowFee;
            OverdueFine = 0m;
        }

        public int DaysOverdue()
        {
            if (IsReturned || DateTime.Now <= DueDate)
                return 0;

            return (DateTime.Now - DueDate).Days;
        }

        public decimal CalculateOverdueFine(decimal finePerDay = 50m)
        {
            int daysOverdue = DaysOverdue();
            return daysOverdue > 0 ? daysOverdue * finePerDay : 0m;
        }

        public override string ToString()
        {
            string status = IsReturned ? "✅ Returned" : "🔄 Active";
            string overdue = DaysOverdue() > 0 ? $" (⚠️ {DaysOverdue()} days overdue)" : "";
            return $"[{Id}] {BookTitle ?? $"Book #{BookId}"} → {PatronName ?? $"Patron #{PatronId}"} - {status}{overdue}";
        }

        public string GetDetailedInfo()
        {
            int daysOverdue = DaysOverdue();
            decimal totalFine = CalculateOverdueFine();

            return $@"
═══════════════════════════════════════
🔄 Loan Details
═══════════════════════════════════════
Loan ID:      {Id}
Book:         {BookTitle ?? $"ID #{BookId}"}
Patron:       {PatronName ?? $"ID #{PatronId}"}
Borrowed:     {BorrowedDate:yyyy-MM-dd}
Due Date:     {DueDate:yyyy-MM-dd}
Return Date:  {(ReturnDate.HasValue ? ReturnDate.Value.ToString("yyyy-MM-dd") : "Not returned")}
Status:       {(IsReturned ? "✅ Returned" : "🔄 Active")}
Borrow Fee:   ₦{BorrowFee:N2}
Days Overdue: {daysOverdue}
Overdue Fine: ₦{totalFine:N2}
Total Due:    ₦{BorrowFee + totalFine:N2}
═══════════════════════════════════════";
        }
    }
}
