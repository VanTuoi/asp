using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APPMVC.Models
{
    public enum Role {
        ADMIN,
        USER
    }

    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên tài khoản không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên tài khoản không được vượt quá 100 ký tự.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public string PasswordHash { get; set; } = string.Empty;

        public List<Role> roles { get; set; } = new List<Role>();
    }
}
