# 🚀 LibraryOS - Complete Setup Guide

## Step 1: Install .NET SDK

### Windows
1. Download .NET 8.0 SDK from: https://dotnet.microsoft.com/download
2. Run the installer
3. Verify installation:
   ```bash
   dotnet --version
   ```

### macOS
```bash
brew install dotnet-sdk
```

### Linux (Ubuntu/Debian)
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
```

## Step 2: Setup Neon Database

1. **Create Account**
   - Go to https://neon.tech
   - Sign up for free account

2. **Create Database**
   - Click "Create Project"
   - Choose a name (e.g., "library-db")
   - Select region closest to you
   - Click "Create Project"

3. **Get Connection String**
   - After creation, copy the connection string shown
   - It looks like:
     ```
     postgresql://username:password@host.neon.tech/dbname?sslmode=require
     ```
   - Convert it to Npgsql format:
     ```
     Host=host.neon.tech;Username=username;Password=password;Database=dbname;SSL Mode=Require
     ```

4. **Run Database Schema**
   - In Neon dashboard, go to "SQL Editor"
   - Copy contents from `database_schema.sql`
   - Paste and run to create tables

## Step 3: Configure the Application

1. **Open Program.cs**
2. **Replace the connection string** on line 11:
   ```csharp
   string connectionString = "Host=YOUR_HOST;Username=YOUR_USER;Password=YOUR_PASS;Database=YOUR_DB;SSL Mode=Require";
   ```
   
   With your actual Neon connection string:
   ```csharp
   string connectionString = "Host=ep-cool-cloud-123456.us-east-2.aws.neon.tech;Username=myuser;Password=mypass123;Database=library;SSL Mode=Require";
   ```

## Step 4: Build and Run

### First Time Setup
```bash
# Navigate to project folder
cd LibraryOS

# Restore packages
dotnet restore

# Build project
dotnet build

# Run application
dotnet run
```

### Subsequent Runs
```bash
dotnet run
```

## Step 5: Compile to EXE (Optional)

### For Windows (64-bit)
```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output will be in: `bin/Release/net8.0/win-x64/publish/LibraryOS.exe`

### For Linux (64-bit)
```bash
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

### For macOS (64-bit)
```bash
dotnet publish -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true
```

## Troubleshooting

### "dotnet: command not found"
- Make sure .NET SDK is installed
- Restart terminal after installation
- Check PATH environment variable

### "Database connection failed"
- Verify connection string is correct
- Check internet connection
- Ensure Neon database is active
- Try pinging the Neon host

### "Npgsql package not found"
```bash
dotnet add package Npgsql
dotnet restore
```

### SSL/TLS Connection Issues
- Make sure "SSL Mode=Require" is in connection string
- Update Npgsql package: `dotnet add package Npgsql --version 8.0.1`

## Distribution

### Share the EXE
1. Compile to EXE (see Step 5)
2. The compiled file is standalone - no .NET installation needed
3. Share via:
   - Google Drive
   - GitHub Releases
   - USB drive
   - Direct download link

### Create Installer (Advanced)
Use tools like:
- Inno Setup (Windows)
- WiX Toolset
- ClickOnce

## Testing the Application

1. **First Launch**
   - App will create database tables automatically
   - You'll see "Database tables initialized successfully!"

2. **Add Sample Data**
   - Add a few books
   - Register some patrons
   - Test borrowing/returning

3. **Test Features**
   - Search functionality
   - Availability checking
   - Overdue tracking
   - Reports

## Next Steps

### Enhancements You Can Add
- [ ] Admin login system
- [ ] Email notifications for overdue books
- [ ] Barcode scanning integration
- [ ] Export reports to PDF/CSV
- [ ] GUI version (WPF/Avalonia)
- [ ] Web version (ASP.NET Core)
- [ ] Mobile app (MAUI)

### Learning Resources
- C# Documentation: https://docs.microsoft.com/dotnet/csharp/
- Npgsql Documentation: https://www.npgsql.org/doc/
- PostgreSQL Tutorial: https://www.postgresqltutorial.com/

## Support

If you encounter issues:
1. Check the error message carefully
2. Verify database connection
3. Ensure all packages are installed
4. Check .NET version compatibility

## Credits

Built with:
- C# / .NET 8.0
- PostgreSQL (Neon)
- Npgsql
- Console UI

Happy coding! 🚀
