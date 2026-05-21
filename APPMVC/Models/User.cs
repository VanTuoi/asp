using System.Collections.Generic;

namespace APPMVC.Models
{
    public enum Role {
        ADMIN,
        USER
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Gender { get; set; } = "Nam";
        public string PasswordHash { get; set; } = string.Empty;
        public List<Role> roles { get; set; } = new List<Role>();
    }
}
