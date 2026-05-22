using System.ComponentModel.DataAnnotations;

namespace APPMVC.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống.")]
        [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email không vượt quá 100 ký tự.")]
        public string Email { get; set; } = string.Empty;

        // [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        // [RegularExpression(@"^0\d{9}$", ErrorMessage = "SĐT phải bắt đầu bằng 0 và gồm 10 chữ số.")]
        // public string PhoneNumber { get; set; } = string.Empty;

        // [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        // [StringLength(50, ErrorMessage = "Tên đăng nhập không vượt quá 50 ký tự.")]
        // public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6-100 ký tự.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string Gender { get; set; } = "Nam";

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        public string Role { get; set; } = "USER";
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email không được để trống.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        // [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        // public string PhoneNumber { get; set; } = string.Empty;

        // [Required(ErrorMessage = "Tên đăng nhập không được để trống.")]
        // public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu không được để trống.")]
        public string Password { get; set; } = string.Empty;
    }
}
