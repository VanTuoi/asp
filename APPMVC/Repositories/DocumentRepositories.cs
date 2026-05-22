using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using APPMVC.Models;

namespace APPMVC.Repositories
{
    public class DocumentRepositories
    {
        private readonly string _connectionString;
        public DocumentRepositories(IConfiguration config) => 
            _connectionString = config.GetConnectionString("DefaultConnection")!;

        public List<Document> GetDocuments(string? search = null)
        {
            var docs = new List<Document>();
            var query = @"
                SELECT d.*, u.Name AS AuthorName
                FROM Documents d
                INNER JOIN Users u ON d.UserId = u.Id
                WHERE d.Title LIKE @Search OR d.Description LIKE @Search OR @Search IS NULL
                ORDER BY d.UploadedAt DESC";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Search", string.IsNullOrEmpty(search) ? DBNull.Value : $"%{search}%");

            conn.Open();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                docs.Add(new Document
                {
                    Id = (int)r["Id"],
                    Title = (string)r["Title"],
                    Description = r["Description"] as string,
                    FilePath = (string)r["FilePath"],
                    FileName = (string)r["FileName"],
                    UploadedAt = (DateTime)r["UploadedAt"],
                    UserId = (int)r["UserId"],
                    AuthorName = (string)r["AuthorName"]
                });
            }
            return docs;
        }

        public Document? GetDocumentById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                SELECT d.*, u.Name AS AuthorName 
                FROM Documents d 
                INNER JOIN Users u ON d.UserId = u.Id 
                WHERE d.Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            conn.Open();
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new Document
            {
                Id = (int)r["Id"],
                Title = (string)r["Title"],
                Description = r["Description"] as string,
                FilePath = (string)r["FilePath"],
                FileName = (string)r["FileName"],
                UploadedAt = (DateTime)r["UploadedAt"],
                UserId = (int)r["UserId"],
                AuthorName = (string)r["AuthorName"]
            };
        }

        public bool CreateDocument(Document doc)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(@"
                INSERT INTO Documents (Title, Description, FilePath, FileName, UploadedAt, UserId)
                VALUES (@Title, @Description, @FilePath, @FileName, @UploadedAt, @UserId)", conn);
            
            cmd.Parameters.AddWithValue("@Title", doc.Title);
            cmd.Parameters.AddWithValue("@Description", (object?)doc.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FilePath", doc.FilePath);
            cmd.Parameters.AddWithValue("@FileName", doc.FileName);
            cmd.Parameters.AddWithValue("@UploadedAt", doc.UploadedAt);
            cmd.Parameters.AddWithValue("@UserId", doc.UserId);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateDocument(Document doc)
        {
            var query = @"
                UPDATE Documents 
                SET Title = @Title, Description = @Description";
            
            if (!string.IsNullOrEmpty(doc.FilePath))
                query += ", FilePath = @FilePath, FileName = @FileName";
            
            query += " WHERE Id = @Id";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(query, conn);
            
            cmd.Parameters.AddWithValue("@Title", doc.Title);
            cmd.Parameters.AddWithValue("@Description", (object?)doc.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", doc.Id);
            
            if (!string.IsNullOrEmpty(doc.FilePath))
            {
                cmd.Parameters.AddWithValue("@FilePath", doc.FilePath);
                cmd.Parameters.AddWithValue("@FileName", doc.FileName);
            }

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteDocument(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("DELETE FROM Documents WHERE Id = @Id", conn);
            cmd.Parameters.AddWithValue("@Id", id);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
