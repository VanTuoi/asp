using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using APPMVC.Models;

namespace APPMVC.Repositories
{
    public class DocumentRepositories
    {
        private readonly string _connectionString;

        public DocumentRepositories(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("DefaultConnection string is missing");
        }

        public List<Category> GetCategories()
        {
            var categories = new List<Category>();
            var query = "SELECT Id, Name, Description FROM Categories ORDER BY Name";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            connection.Open();

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(new Category
                {
                    Id = (int)reader["Id"],
                    Name = (string)reader["Name"],
                    Description = reader["Description"] as string
                });
            }
            return categories;
        }

        public List<Document> GetDocuments(string? search = null, int? categoryId = null)
        {
            var docs = new List<Document>();
            var query = @"
                SELECT d.Id, d.Title, d.Description, d.FilePath, d.FileName, d.UploadedAt,
                       d.CategoryId, c.Name AS CategoryName,
                       d.UserId, u.Name AS AuthorName
                FROM Documents d
                INNER JOIN Categories c ON d.CategoryId = c.Id
                INNER JOIN Users u ON d.UserId = u.Id
                WHERE 1=1";

            if (!string.IsNullOrEmpty(search))
            {
                query += " AND (d.Title LIKE @Search OR d.Description LIKE @Search)";
            }
            if (categoryId.HasValue && categoryId > 0)
            {
                query += " AND d.CategoryId = @CategoryId";
            }
            query += " ORDER BY d.UploadedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            
            if (!string.IsNullOrEmpty(search))
            {
                command.Parameters.AddWithValue("@Search", $"%{search}%");
            }
            if (categoryId.HasValue && categoryId > 0)
            {
                command.Parameters.AddWithValue("@CategoryId", categoryId.Value);
            }

            connection.Open();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                docs.Add(new Document
                {
                    Id = (int)reader["Id"],
                    Title = (string)reader["Title"],
                    Description = reader["Description"] as string,
                    FilePath = (string)reader["FilePath"],
                    FileName = (string)reader["FileName"],
                    UploadedAt = (DateTime)reader["UploadedAt"],
                    CategoryId = (int)reader["CategoryId"],
                    CategoryName = (string)reader["CategoryName"],
                    UserId = (int)reader["UserId"],
                    AuthorName = (string)reader["AuthorName"]
                });
            }
            return docs;
        }

        public Document? GetDocumentById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetDocumentDetails", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@DocumentId", id);

            connection.Open();
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Document
                {
                    Id = (int)reader["Id"],
                    Title = (string)reader["Title"],
                    Description = reader["Description"] as string,
                    FilePath = (string)reader["FilePath"],
                    FileName = (string)reader["FileName"],
                    UploadedAt = (DateTime)reader["UploadedAt"],
                    CategoryId = (int)reader["CategoryId"],
                    CategoryName = (string)reader["CategoryName"],
                    UserId = (int)reader["UserId"],
                    AuthorName = (string)reader["AuthorName"]
                };
            }
            return null;
        }

        public bool CreateDocument(Document doc)
        {
            var query = @"
                INSERT INTO Documents (Title, Description, FilePath, FileName, UploadedAt, CategoryId, UserId)
                VALUES (@Title, @Description, @FilePath, @FileName, @UploadedAt, @CategoryId, @UserId)";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                using var command = new SqlCommand(query, connection, transaction);
                command.Parameters.AddWithValue("@Title", doc.Title);
                command.Parameters.AddWithValue("@Description", (object?)doc.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@FilePath", doc.FilePath);
                command.Parameters.AddWithValue("@FileName", doc.FileName);
                command.Parameters.AddWithValue("@UploadedAt", doc.UploadedAt);
                command.Parameters.AddWithValue("@CategoryId", doc.CategoryId);
                command.Parameters.AddWithValue("@UserId", doc.UserId);
                command.ExecuteNonQuery();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool UpdateDocument(Document doc)
        {
            var query = @"
                UPDATE Documents 
                SET Title = @Title, 
                    Description = @Description, 
                    CategoryId = @CategoryId";

            if (!string.IsNullOrEmpty(doc.FilePath) && !string.IsNullOrEmpty(doc.FileName))
            {
                query += ", FilePath = @FilePath, FileName = @FileName";
            }
            
            query += " WHERE Id = @Id";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@Title", doc.Title);
            command.Parameters.AddWithValue("@Description", (object?)doc.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", doc.CategoryId);
            command.Parameters.AddWithValue("@Id", doc.Id);

            if (!string.IsNullOrEmpty(doc.FilePath) && !string.IsNullOrEmpty(doc.FileName))
            {
                command.Parameters.AddWithValue("@FilePath", doc.FilePath);
                command.Parameters.AddWithValue("@FileName", doc.FileName);
            }

            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }

        public bool DeleteDocument(int id)
        {
            var query = "DELETE FROM Documents WHERE Id = @Id";
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@Id", id);

            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }
    }
}
