namespace LibraryOS.Models
{
    /// <summary>
    /// Represents a library patron (user)
    /// </summary>
    public class Patron
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? Phone { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int MaxBooks { get; set; }
        public bool IsActive { get; set; }

        public Patron()
        {
            Name = string.Empty;
            Email = string.Empty;
            RegistrationDate = DateTime.Now;
            MaxBooks = 3;
            IsActive = true;
        }

        public Patron(string name, string email, string? phone = null)
        {
            Name = name;
            Email = email;
            Phone = phone;
            RegistrationDate = DateTime.Now;
            MaxBooks = 3;
            IsActive = true;
        }

        public override string ToString()
        {
            string status = IsActive ? "✅ Active" : "❌ Inactive";
            return $"[{Id}] {Name} ({Email}) - {status}";
        }

        public string GetDetailedInfo()
        {
            return $@"
═══════════════════════════════════════
👤 Patron Details
═══════════════════════════════════════
ID:          {Id}
Name:        {Name}
Email:       {Email}
Phone:       {Phone ?? "N/A"}
Max Books:   {MaxBooks}
Status:      {(IsActive ? "✅ Active" : "❌ Inactive")}
Joined:      {RegistrationDate:yyyy-MM-dd}
═══════════════════════════════════════";
        }
    }
}
