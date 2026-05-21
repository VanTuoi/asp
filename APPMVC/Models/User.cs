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

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (phải bắt đầu bằng số 0 và gồm 10 chữ số).")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public string PasswordHash { get; set; } = string.Empty;

        public List<Role> roles { get; set; } = new List<Role>();
    }
}
