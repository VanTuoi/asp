using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APPMVC.Models
{
    public class PostViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        public string Content { get; set; } = string.Empty;

        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
