using System.ComponentModel.DataAnnotations;

namespace APPMVC.Models
{
    public class Attachment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên file không được để trống.")]
        [StringLength(255, ErrorMessage = "Tên file không được vượt quá 255 ký tự.")]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đường dẫn file không được để trống.")]
        [StringLength(500, ErrorMessage = "Đường dẫn file không được vượt quá 500 ký tự.")]
        public string FilePath { get; set; } = string.Empty;

        public int PostId { get; set; }
    }
}
