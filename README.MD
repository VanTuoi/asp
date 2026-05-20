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

-- Bảng Users
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        UserName NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(255) NOT NULL,
        Roles NVARCHAR(255) NOT NULL
    );
END
GO

-- Bảng Posts
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Posts')
BEGIN
    CREATE TABLE Posts (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(255) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UserId INT NOT NULL,
        CONSTRAINT FK_Posts_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END
GO

-- Bảng Attachments
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Attachments')
BEGIN
    CREATE TABLE Attachments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        PostId INT NOT NULL,
        CONSTRAINT FK_Attachments_Posts FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE
    );
END
GO

-- Stored Procedure lấy thông tin bài viết và đính kèm
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetPostDetails')
    DROP PROCEDURE sp_GetPostDetails;
GO
CREATE PROCEDURE sp_GetPostDetails
    @PostId INT
AS
BEGIN
    SET NOCOUNT ON;
    -- 1. Lấy chi tiết bài viết (INNER JOIN lấy tên tác giả)
    SELECT p.Id, p.Title, p.Content, p.CreatedAt, p.UserId, u.Name AS AuthorName
    FROM Posts p
    INNER JOIN Users u ON p.UserId = u.Id
    WHERE p.Id = @PostId;

    -- 2. Lấy danh sách file đính kèm
    SELECT Id, FileName, FilePath, PostId FROM Attachments WHERE PostId = @PostId;
END
GO
```

## 3. Khởi chạy ứng dụng

```bash
dotnet watch run
```

// CMD
for /f "delims=" %f in ('dir /b /s .\APPMVC\*.cs .\APPMVC\*.cshtml .\APPMVC\*.json ^| findstr /v "\\bin\\" ^| findstr /v "\\obj\\"') do @echo. & @echo --- FILE: %~nxf --- & @type "%f") > AllCode.txt

// PowerShell
Get-ChildItem -Path .\APPMVC -File -Recurse | Where-Object { ($_.Extension -in ".cs", ".cshtml", ".json") -and ($_.FullName -notmatch "\\bin\\|\\obj\\") } | ForEach-Object { "`n`n--- FILE: $($_.Name) ---`n" + (Get-Content $\_.FullName -Raw) } | Out-File AllCode.txt -Encoding utf8
