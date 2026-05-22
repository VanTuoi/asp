using System.Security.Cryptography;
using System.Text;
using APPMVC.Models;
using Microsoft.Data.SqlClient;

namespace APPMVC.Repositories
{
    public class UserRepositories
    {
        private readonly string _connectionString;
        public UserRepositories(IConfiguration config) => 
            _connectionString = config.GetConnectionString("DefaultConnection")!;

        public string HashPassword(string pwd) => 
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(pwd)));

        public bool Register(User user, string password)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(@"
                    INSERT INTO Users (Name, Email, Gender, PasswordHash, Roles)
                    VALUES (@Name, @Email, @Gender, @PasswordHash, @Roles)", conn);
                
                cmd.Parameters.AddWithValue("@Name", user.Name);
                cmd.Parameters.AddWithValue("@Email", user.Email);
                cmd.Parameters.AddWithValue("@Gender", user.Gender);
                cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                cmd.Parameters.AddWithValue("@Roles", string.Join(",", user.roles));

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch { return false; }
        }

        public User? Login(string email, string password)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("SELECT * FROM Users WHERE Email = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);
            
            conn.Open();
            using var r = cmd.ExecuteReader();
            if (!r.Read() || (string)r["PasswordHash"] != HashPassword(password)) return null;

            var rolesStr = r["Roles"] as string;
            return new User
            {
                Id = (int)r["Id"],
                Name = (string)r["Name"],
                Email = (string)r["Email"],
                Gender = (string)r["Gender"],
                roles = string.IsNullOrEmpty(rolesStr) ? [] : rolesStr.Split(',').Select(Enum.Parse<Role>).ToList()
            };
        }
    }
}
