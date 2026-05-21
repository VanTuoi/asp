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
        Email NVARCHAR(100) NOT NULL UNIQUE,
        PhoneNumber VARCHAR(20) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        Roles NVARCHAR(255) NOT NULL
    );
END
GO

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

IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetPostDetails')
    DROP PROCEDURE sp_GetPostDetails;
GO
CREATE PROCEDURE sp_GetPostDetails
    @PostId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.Id, p.Title, p.Content, p.CreatedAt, p.UserId, u.Name AS AuthorName
    FROM Posts p
    INNER JOIN Users u ON p.UserId = u.Id
    WHERE p.Id = @PostId;

    SELECT Id, FileName, FilePath, PostId FROM Attachments WHERE PostId = @PostId;
END
GO
