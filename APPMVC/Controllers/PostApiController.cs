using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using APPMVC.Models;
using APPMVC.Repositories;
using APPMVC.Helpers;

namespace APPMVC.Controllers
{
    [Authorize]
    public class PostApiController : Controller
    {
        private readonly PostRepositories _postRepository;
        private readonly IWebHostEnvironment _env;

        public PostApiController(PostRepositories postRepository, IWebHostEnvironment env)
        {
            _postRepository = postRepository;
            _env = env;
        }

        // 1. GET: Lấy danh sách bài viết dưới dạng JSON (URL: /PostApi/GetPosts)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetPosts()
        {
            var posts = _postRepository.GetPosts();
            return Json(posts);
        }

        // 2. GET: Lấy thông tin chi tiết bài viết (kèm attachments) dưới dạng JSON (URL: /PostApi/GetPost?id=...)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetPost(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại." });
            }
            return Json(post);
        }

        // 3. POST: Tạo bài viết mới kèm upload file an toàn (URL: /PostApi/CreatePost)
        [HttpPost]
        public async Task<IActionResult> CreatePost(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Dữ liệu không hợp lệ.", errors });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });
            }

            post.CreatedAt = DateTime.Now;
            post.UserId = userId;

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
            long maxFileSize = 5 * 1024 * 1024; // 5 MB

            if (files != null && files.Count > 0)
            {
                // Bước 1: Validate toàn bộ files trước
                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    if (file.Length > maxFileSize)
                    {
                        return Json(new { success = false, message = $"File {file.FileName} vượt quá dung lượng cho phép (Max 5MB)." });
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    {
                        return Json(new { success = false, message = $"File {file.FileName} không đúng định dạng (.jpg, .png, .pdf, .doc, .docx)." });
                    }

                    if (!FileHelper.ValidateFileSignature(file, ext))
                    {
                        return Json(new { success = false, message = $"Nội dung file {file.FileName} không trùng khớp định dạng thực tế." });
                    }
                }

                // Bước 2: Lưu files vật lý và gắn vào attachments list
                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    var uniqueName = $"{Guid.NewGuid()}{ext}";
                    var uploadDir = Path.Combine(_env.WebRootPath, "uploads");

                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var uploadPath = Path.Combine(uploadDir, uniqueName);
                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    post.Attachments.Add(new Attachment
                    {
                        FileName = file.FileName,
                        FilePath = "/uploads/" + uniqueName
                    });
                }
            }

            bool success = _postRepository.CreatePost(post);
            if (success)
            {
                return Json(new { success = true, message = "Tạo bài viết thành công!", postId = post.Id });
            }
            return Json(new { success = false, message = "Không thể lưu bài viết vào CSDL." });
        }

        // 4. POST: Cập nhật bài viết kèm upload file an toàn (URL: /PostApi/UpdatePost)
        [HttpPost]
        public async Task<IActionResult> UpdatePost(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var existingPost = _postRepository.GetPostById(post.Id);
            if (existingPost == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("ADMIN");

            if (existingPost.UserId.ToString() != userIdClaim && !isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền cập nhật bài viết này." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
            long maxFileSize = 5 * 1024 * 1024; // 5 MB

            if (files != null && files.Count > 0)
            {
                // Bước 1: Validate files trước
                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    if (file.Length > maxFileSize)
                    {
                        return Json(new { success = false, message = $"File {file.FileName} vượt quá dung lượng cho phép (Max 5MB)." });
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    {
                        return Json(new { success = false, message = $"File {file.FileName} không đúng định dạng (.jpg, .png, .pdf, .doc, .docx)." });
                    }

                    if (!FileHelper.ValidateFileSignature(file, ext))
                    {
                        return Json(new { success = false, message = $"Nội dung file {file.FileName} không trùng khớp định dạng thực tế." });
                    }
                }
            }

            bool success = _postRepository.UpdatePost(post);
            if (success)
            {
                if (files != null && files.Count > 0)
                {
                    // Bước 2: Lưu files và lưu Database Attachments
                    foreach (var file in files)
                    {
                        if (file == null || file.Length == 0) continue;

                        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                        var uniqueName = $"{Guid.NewGuid()}{ext}";
                        var uploadDir = Path.Combine(_env.WebRootPath, "uploads");

                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        var uploadPath = Path.Combine(uploadDir, uniqueName);
                        using (var stream = new FileStream(uploadPath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var attachment = new Attachment
                        {
                            FileName = file.FileName,
                            FilePath = "/uploads/" + uniqueName,
                            PostId = post.Id
                        };

                        _postRepository.AddAttachment(attachment);
                    }
                }
                return Json(new { success = true, message = "Cập nhật bài viết thành công!" });
            }
            return Json(new { success = false, message = "Cập nhật thất bại." });
        }

        // 5. POST: Xóa bài viết (Dạng AJAX/JSON) (URL: /PostApi/DeleteAjax?id=...)
        [HttpPost]
        public IActionResult DeleteAjax(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("ADMIN");

            if (post.UserId.ToString() != userIdClaim && !isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa bài viết này." });
            }

            // Xóa file vật lý trước
            foreach (var attach in post.Attachments)
            {
                var fullPath = Path.Combine(_env.WebRootPath, attach.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            bool success = _postRepository.DeletePost(id);
            if (success)
            {
                return Json(new { success = true, message = "Xóa bài viết thành công!" });
            }
            return Json(new { success = false, message = "Xóa bài viết thất bại." });
        }

        // 6. POST: Xóa file đính kèm đơn lẻ (Dạng AJAX/JSON) (URL: /PostApi/DeleteAttachmentAjax?id=...)
        [HttpPost]
        public IActionResult DeleteAttachmentAjax(int id)
        {
            var attachment = _postRepository.GetAttachmentById(id);
            if (attachment == null)
            {
                return Json(new { success = false, message = "File đính kèm không tồn tại." });
            }

            var post = _postRepository.GetPostById(attachment.PostId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không hợp lệ." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("ADMIN");

            if (post.UserId.ToString() != userIdClaim && !isAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa file đính kèm này." });
            }

            // Xóa file vật lý trước
            var fullPath = Path.Combine(_env.WebRootPath, attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            // Xóa DB
            bool success = _postRepository.DeleteAttachment(id);
            if (success)
            {
                return Json(new { success = true, message = "Xóa file thành công!" });
            }
            return Json(new { success = false, message = "Không thể xóa file khỏi CSDL." });
        }
    }
}
