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

        // 1. Lấy danh sách bài viết kèm tên tác giả (Sử dụng INNER JOIN)
        public List<Post> GetPosts()
        {
            var posts = new List<Post>();
            var query = @"
                SELECT p.Id, p.Title, p.Content, p.CreatedAt, p.UserId, u.Name AS AuthorName 
                FROM Posts p
                INNER JOIN Users u ON p.UserId = u.Id
                ORDER BY p.CreatedAt DESC";

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            posts.Add(new Post
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Content = reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3),
                                UserId = reader.GetInt32(4),
                                AuthorName = reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return posts;
        }

        // 2. Lấy chi tiết bài viết và danh sách file đính kèm (Sử dụng Stored Procedure & Multiple Result Sets)
        public Post? GetPostById(int id)
        {
            Post? post = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_GetPostDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@PostId", id);

                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        // Result set 1: Thông tin bài viết
                        if (reader.Read())
                        {
                            post = new Post
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Content = reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3),
                                UserId = reader.GetInt32(4),
                                AuthorName = reader.GetString(5)
                            };
                        }

                        // Result set 2: Danh sách file đính kèm (sử dụng NextResult)
                        if (post != null && reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                post.Attachments.Add(new Attachment
                                {
                                    Id = reader.GetInt32(0),
                                    FileName = reader.GetString(1),
                                    FilePath = reader.GetString(2),
                                    PostId = reader.GetInt32(3)
                                });
                            }
                        }
                    }
                }
            }
            return post;
        }

        // 3. Thêm bài viết mới và danh sách đính kèm sử dụng Transaction (COMMIT / ROLLBACK)
        public bool CreatePost(Post post)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Ghi bài viết và nhận lại ID vừa sinh tự động
                        var postQuery = @"
                            INSERT INTO Posts (Title, Content, CreatedAt, UserId) 
                            OUTPUT INSERTED.Id 
                            VALUES (@Title, @Content, @CreatedAt, @UserId)";

                        int newPostId;
                        using (var cmd = new SqlCommand(postQuery, connection, transaction))
                        {
                            // Phòng chống SQL Injection bằng Parameters
                            cmd.Parameters.AddWithValue("@Title", post.Title);
                            cmd.Parameters.AddWithValue("@Content", post.Content);
                            cmd.Parameters.AddWithValue("@CreatedAt", post.CreatedAt);
                            cmd.Parameters.AddWithValue("@UserId", post.UserId);

                            newPostId = (int)cmd.ExecuteScalar();
                        }

                        // Ghi danh sách file đính kèm
                        foreach (var attach in post.Attachments)
                        {
                            var attachQuery = @"
                                INSERT INTO Attachments (FileName, FilePath, PostId) 
                                VALUES (@FileName, @FilePath, @PostId)";

                            using (var cmd = new SqlCommand(attachQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@FileName", attach.FileName);
                                cmd.Parameters.AddWithValue("@FilePath", attach.FilePath);
                                cmd.Parameters.AddWithValue("@PostId", newPostId);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Hoàn tất giao dịch thành công
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        // Thu hồi toàn bộ nếu xảy ra bất kỳ lỗi gì
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        // 4. Xóa bài viết (Xóa file đính kèm trước để tránh lỗi khóa ngoại)
        public bool DeletePost(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Xóa các đính kèm trước
                        var deleteAttachQuery = "DELETE FROM Attachments WHERE PostId = @PostId";
                        using (var cmd = new SqlCommand(deleteAttachQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@PostId", id);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. Xóa bài viết
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
            }
        }

        // 5. Cập nhật bài viết (UPDATE DML)
        public bool UpdatePost(Post post)
        {
            var query = "UPDATE Posts SET Title = @Title, Content = @Content WHERE Id = @Id";
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Title", post.Title);
                    command.Parameters.AddWithValue("@Content", post.Content);
                    command.Parameters.AddWithValue("@Id", post.Id);

                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        // 6. Lấy thông tin 1 file đính kèm bằng ID
        public Attachment? GetAttachmentById(int id)
        {
            var query = "SELECT Id, FileName, FilePath, PostId FROM Attachments WHERE Id = @Id";
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Attachment
                            {
                                Id = reader.GetInt32(0),
                                FileName = reader.GetString(1),
                                FilePath = reader.GetString(2),
                                PostId = reader.GetInt32(3)
                            };
                        }
                    }
                }
            }
            return null;
        }

        // 7. Xóa 1 file đính kèm trong DB
        public bool DeleteAttachment(int id)
        {
            var query = "DELETE FROM Attachments WHERE Id = @Id";
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        // 8. Thêm mới 1 file đính kèm lẻ
        public bool AddAttachment(Attachment attachment)
        {
            var query = "INSERT INTO Attachments (FileName, FilePath, PostId) VALUES (@FileName, @FilePath, @PostId)";
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FileName", attachment.FileName);
                    command.Parameters.AddWithValue("@FilePath", attachment.FilePath);
                    command.Parameters.AddWithValue("@PostId", attachment.PostId);
                    connection.Open();
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }
    }
}
