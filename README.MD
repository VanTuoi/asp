# APPMVC

## 1. Cấu hình Connection String (`appsettings.json`)

- **Cách 1: Windows Authentication (Xác thực Windows)**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=APPMVC;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

- **Cách 2: SQL Server Authentication (Tài khoản sa/sa2008)**

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=APPMVC;User Id=sa;Password=sa2008;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

## 2. SQL Script Khởi tạo (Chạy trong SSMS)

```sql
USE master;
GO
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'APPMVC')
    CREATE DATABASE APPMVC;
GO
USE APPMVC;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Email NVARCHAR(150) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(64) NOT NULL,
        Gender NVARCHAR(10) NULL,
        Roles NVARCHAR(50) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Documents')
BEGIN
    CREATE TABLE Documents (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX) NULL,
        FilePath NVARCHAR(500) NOT NULL,
        FileName NVARCHAR(250) NOT NULL,
        UploadedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UserId INT NOT NULL,
        CONSTRAINT FK_Documents_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END
GO
```

## 3. Khởi chạy ứng dụng

```bash
dotnet watch run
```

### Lệnh copy code vào file

// CMD
for /f "delims=" %f in ('dir /b /s .\APPMVC\*.cs .\APPMVC\*.cshtml .\APPMVC\*.json ^| findstr /v "\\bin\\" ^| findstr /v "\\obj\\"') do @echo. & @echo --- FILE: %~nxf --- & @type "%f" >> AllCode.txt

// PowerShell
Get-ChildItem -Path .\APPMVC -File -Recurse |
Where-Object {
($_.Extension -in ".cs", ".cshtml", ".json") -and
    ($_.FullName -notmatch "\\bin\\|\\obj\\")
} |
ForEach-Object {
"`n--- FILE: $($_.Name) ---`n" + (Get-Content $\_.FullName -Raw)
} |
Out-File AllCode.txt -Encoding utf8
