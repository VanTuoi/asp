using System.Security.Cryptography;
using System.Text;
using APPMVC.Models;
using Microsoft.Data.SqlClient;

namespace APPMVC.Repositories
{
    public class UserRepositories
    {
        private readonly string _connectionString;

        public UserRepositories(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new ArgumentNullException("DefaultConnection string is missing");
        }

        public string HashPassword(string password)
        {
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        public bool Register(User user, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Check if username exists
                var checkQuery = "SELECT COUNT(1) FROM Users WHERE UserName = @UserName";
                using (var checkCmd = new SqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("@UserName", user.UserName);
                    int exists = (int)checkCmd.ExecuteScalar()!;
                    if (exists > 0)
                    {
                        return false;
                    }
                }

                // Insert user
                var insertQuery = @"
                    INSERT INTO Users (Name, UserName, PasswordHash, Roles)
                    VALUES (@Name, @UserName, @PasswordHash, @Roles)";
                
                string passwordHash = HashPassword(password);
                string rolesStr = string.Join(",", user.roles.Select(r => r.ToString()));

                using (var insertCmd = new SqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("@Name", user.Name);
                    insertCmd.Parameters.AddWithValue("@UserName", user.UserName);
                    insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    insertCmd.Parameters.AddWithValue("@Roles", rolesStr);

                    insertCmd.ExecuteNonQuery();
                }
            }
            return true;
        }

        public User? Login(string username, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT Id, Name, UserName, PasswordHash, Roles FROM Users WHERE UserName = @UserName";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserName", username);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string dbPasswordHash = reader.GetString(3);
                            string inputHash = HashPassword(password);

                            if (dbPasswordHash == inputHash)
                            {
                                var user = new User
                                {
                                    Id = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    UserName = reader.GetString(2),
                                    PasswordHash = dbPasswordHash,
                                    roles = new List<Role>()
                                };

                                string rolesStr = reader.GetString(4);
                                if (!string.IsNullOrEmpty(rolesStr))
                                {
                                    foreach (var r in rolesStr.Split(','))
                                    {
                                        if (Enum.TryParse<Role>(r, out var role))
                                        {
                                            user.roles.Add(role);
                                        }
                                    }
                                }
                                return user;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
