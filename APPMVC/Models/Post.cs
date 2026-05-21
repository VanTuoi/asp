using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace APPMVC.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        [StringLength(255, ErrorMessage = "Tiêu đề không quá 255 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống.")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        public string AuthorName { get; set; } = string.Empty;
        
        public List<Attachment> Attachments { get; set; } = new List<Attachment>();
    }
}
