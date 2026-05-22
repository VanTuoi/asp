using System.ComponentModel.DataAnnotations;

namespace APPMVC.Models
{
    public class DocumentViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
        public string? Description { get; set; }

        public string? CurrentFileName { get; set; }
        public string? CurrentFilePath { get; set; }
    }
}
