# 📚 LibraryOS - Quick Reference

## 🚀 Quick Start (3 Steps)

1. **Install .NET 8.0**
   ```bash
   # Download from: https://dotnet.microsoft.com/download
   dotnet --version  # Verify installation
   ```

2. **Setup Neon Database**
   - Create free account: https://neon.tech
   - Create project → Copy connection string
   - Run `database_schema.sql` in SQL Editor

3. **Run Application**
   ```bash
   cd LibraryOS
   dotnet restore
   dotnet run
   ```

## 🔧 Essential Commands

```bash
# Build
dotnet build

# Run
dotnet run

# Compile to EXE (Windows)
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true

# Add package
dotnet add package Npgsql
```

## 📂 Project Structure

```
LibraryOS/
├── Models/           # Book, Patron, Loan classes
├── Services/         # Database & business logic
├── UI/              # Console interface
├── Program.cs       # Entry point
└── database_schema.sql
```

## 🎮 Main Features

### Book Management
- ✅ Add/Remove Books
- ✅ Search by Title/Author
- ✅ Check Availability
- ✅ User Upload (with links)

### Patron Management
- ✅ Register Patrons
- ✅ View Details
- ✅ Track Borrowed Books
- ✅ Borrow Limit (3 books default)

### Loan System
- ✅ Borrow Books
- ✅ Return Books
- ✅ Overdue Tracking
- ✅ Fine Calculation (₦50/day)

### Reports
- ✅ Available Books Count
- ✅ Active Loans
- ✅ Overdue Loans
- ✅ Patron Statistics

## 🔑 Important Configuration

**In Program.cs** (line 11), replace:
```csharp
string connectionString = "Host=YOUR_HOST;Username=USER;Password=PASS;Database=DB;SSL Mode=Require";
```

With your Neon connection string!

## 📊 Database Tables

### books
- id, title, author, isbn
- category, book_link
- is_available, upload_status

### patrons
- id, name, email, phone
- max_books (default: 3)
- is_active

### loans
- book_id, patron_id
- borrowed_date, due_date
- borrow_fee, overdue_fine
- is_returned

## 🎨 UI Features

- **Colored Console** - Cyan headers, Green success, Red errors
- **ASCII Logo** - LibraryOS branding
- **Loading Animations** - Professional feel
- **Input Validation** - Prevents crashes
- **Confirmation Prompts** - Safe deletions

## 💡 Tips

1. **Test Connection First**
   - App auto-tests DB on startup
   - Creates tables if missing

2. **Overdue Fines**
   - ₦50 per day overdue
   - Auto-calculated on return

3. **Book Upload**
   - Users can upload book links
   - Status: pending/approved

4. **Borrow Limits**
   - Default: 3 books per patron
   - Adjustable in database

## 🐛 Common Issues

**"Database connection failed"**
→ Check connection string & internet

**"Npgsql not found"**
→ Run: `dotnet add package Npgsql`

**"dotnet: command not found"**
→ Install .NET SDK & restart terminal

## 📦 Distribution

Compile EXE → Share with anyone → No .NET needed!

Location: `bin/Release/net8.0/win-x64/publish/LibraryOS.exe`

## 🎓 OOP Concepts Used

- ✅ Classes & Objects
- ✅ Encapsulation
- ✅ Inheritance (base classes)
- ✅ Polymorphism (method overriding)
- ✅ Async/Await patterns
- ✅ Dependency Injection
- ✅ Separation of Concerns

---

**Need Help?** Check SETUP_GUIDE.md for detailed instructions!
