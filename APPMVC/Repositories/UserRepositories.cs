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

        public string HashPassword(string password) => 
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

        public bool Register(User user, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var checkQuery = "SELECT COUNT(1) FROM Users WHERE Email = @Email";
            using var checkCmd = new SqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@Email", user.Email);
            
            if ((int)checkCmd.ExecuteScalar()! > 0) return false;

            var insertQuery = @"
                INSERT INTO Users (Name, Email, PhoneNumber, Gender, PasswordHash, Roles)
                VALUES (@Name, @Email, @PhoneNumber, @Gender, @PasswordHash, @Roles)";
            
            using var insertCmd = new SqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@Name", user.Name);
            insertCmd.Parameters.AddWithValue("@Email", user.Email);
            insertCmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
            insertCmd.Parameters.AddWithValue("@Gender", user.Gender);
            insertCmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
            insertCmd.Parameters.AddWithValue("@Roles", string.Join(",", user.roles));

            insertCmd.ExecuteNonQuery();
            return true;
        }

        public User? Login(string email, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            var query = "SELECT Id, Name, Email, PhoneNumber, Gender, PasswordHash, Roles FROM Users WHERE Email = @Email";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Email", email);
            
            using var reader = command.ExecuteReader();
            
            if (!reader.Read()) return null;

            string dbPasswordHash = (string)reader["PasswordHash"];
            
            if (dbPasswordHash != HashPassword(password)) return null;

            string rolesStr = (string)reader["Roles"];
            return new User
            {
                Id = (int)reader["Id"],
                Name = (string)reader["Name"],
                Email = (string)reader["Email"],
                PhoneNumber = (string)reader["PhoneNumber"],
                Gender = (string)reader["Gender"],
                PasswordHash = dbPasswordHash,
                roles = string.IsNullOrEmpty(rolesStr) 
                    ? [] 
                    : rolesStr.Split(',')
                        .Where(r => Enum.TryParse<Role>(r, out _))
                        .Select(Enum.Parse<Role>)
                        .ToList()
            };
        }
    }
}
