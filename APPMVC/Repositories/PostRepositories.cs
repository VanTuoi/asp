using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using APPMVC.Models;

namespace APPMVC.Repositories
{
    public class PostRepositories
    {
        private readonly string _connectionString;

        public PostRepositories(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("DefaultConnection string is missing");
        }

        public List<Post> GetPosts(string? search = null)
        {
            var posts = new List<Post>();
            var query = @"
                SELECT p.Id, p.Title, p.Content, p.CreatedAt, p.UserId, u.Name AS AuthorName 
                FROM Posts p
                INNER JOIN Users u ON p.UserId = u.Id";

            if (!string.IsNullOrEmpty(search))
            {
                query += " WHERE p.Title LIKE @Search OR p.Content LIKE @Search";
            }
            query += " ORDER BY p.CreatedAt DESC";

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            if (!string.IsNullOrEmpty(search))
            {
                command.Parameters.AddWithValue("@Search", $"%{search}%");
            }
            connection.Open();
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                posts.Add(new Post
                {
                    Id = (int)reader["Id"],
                    Title = (string)reader["Title"],
                    Content = (string)reader["Content"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UserId = (int)reader["UserId"],
                    AuthorName = (string)reader["AuthorName"]
                });
            }
            return posts;
        }

        public Post? GetPostById(int id)
        {
            Post? post = null;

            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetPostDetails", connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@PostId", id);

            connection.Open();
            using var reader = command.ExecuteReader();
            
            if (reader.Read())
            {
                post = new Post
                {
                    Id = (int)reader["Id"],
                    Title = (string)reader["Title"],
                    Content = (string)reader["Content"],
                    CreatedAt = (DateTime)reader["CreatedAt"],
                    UserId = (int)reader["UserId"],
                    AuthorName = (string)reader["AuthorName"]
                };
            }

            if (post != null && reader.NextResult())
            {
                while (reader.Read())
                {
                    post.Attachments.Add(new Attachment
                    {
                        Id = (int)reader["Id"],
                        FileName = (string)reader["FileName"],
                        FilePath = (string)reader["FilePath"],
                        PostId = (int)reader["PostId"]
                    });
                }
            }
            return post;
        }

        public bool CreatePost(Post post)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                var postQuery = @"
                    INSERT INTO Posts (Title, Content, CreatedAt, UserId) 
                    OUTPUT INSERTED.Id 
                    VALUES (@Title, @Content, @CreatedAt, @UserId)";

                int newPostId;
                using (var cmd = new SqlCommand(postQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Title", post.Title);
                    cmd.Parameters.AddWithValue("@Content", post.Content);
                    cmd.Parameters.AddWithValue("@CreatedAt", post.CreatedAt);
                    cmd.Parameters.AddWithValue("@UserId", post.UserId);

                    newPostId = (int)cmd.ExecuteScalar();
                }

                foreach (var attach in post.Attachments)
                {
                    var attachQuery = @"
                        INSERT INTO Attachments (FileName, FilePath, PostId) 
                        VALUES (@FileName, @FilePath, @PostId)";

                    using var cmd = new SqlCommand(attachQuery, connection, transaction);
                    cmd.Parameters.AddWithValue("@FileName", attach.FileName);
                    cmd.Parameters.AddWithValue("@FilePath", attach.FilePath);
                    cmd.Parameters.AddWithValue("@PostId", newPostId);

                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool DeletePost(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            
            try
            {
                var deleteAttachQuery = "DELETE FROM Attachments WHERE PostId = @PostId";
                using (var cmd = new SqlCommand(deleteAttachQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@PostId", id);
                    cmd.ExecuteNonQuery();
                }

                var deletePostQuery = "DELETE FROM Posts WHERE Id = @Id";
                using (var cmd = new SqlCommand(deletePostQuery, connection, transaction))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool UpdatePost(Post post)
        {
            var query = "UPDATE Posts SET Title = @Title, Content = @Content WHERE Id = @Id";
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@Title", post.Title);
            command.Parameters.AddWithValue("@Content", post.Content);
            command.Parameters.AddWithValue("@Id", post.Id);

            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }

        public Attachment? GetAttachmentById(int id)
        {
            var query = "SELECT Id, FileName, FilePath, PostId FROM Attachments WHERE Id = @Id";
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@Id", id);
            connection.Open();
            
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;

            return new Attachment
            {
                Id = (int)reader["Id"],
                FileName = (string)reader["FileName"],
                FilePath = (string)reader["FilePath"],
                PostId = (int)reader["PostId"]
            };
        }

        public bool DeleteAttachment(int id)
        {
            var query = "DELETE FROM Attachments WHERE Id = @Id";
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@Id", id);
            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }

        public bool AddAttachment(Attachment attachment)
        {
            var query = "INSERT INTO Attachments (FileName, FilePath, PostId) VALUES (@FileName, @FilePath, @PostId)";
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            
            command.Parameters.AddWithValue("@FileName", attachment.FileName);
            command.Parameters.AddWithValue("@FilePath", attachment.FilePath);
            command.Parameters.AddWithValue("@PostId", attachment.PostId);
            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }
    }
}
