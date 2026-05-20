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
    public class HomeController : Controller
    {
        private readonly PostRepositories _postRepository;
        private readonly IWebHostEnvironment _env;

        public HomeController(PostRepositories postRepository, IWebHostEnvironment env)
        {
            _postRepository = postRepository;
            _env = env;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            var posts = _postRepository.GetPosts();
            return View(posts);
        }

        [AllowAnonymous]
        public IActionResult Details(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                return View(post);
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Challenge();
            }

            post.CreatedAt = DateTime.Now;
            post.UserId = userId;

            if (files != null && files.Count > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                var allowedMimeTypes = new[] { 
                    "image/jpeg", 
                    "image/png", 
                    "application/pdf", 
                    "application/msword", 
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
                };
                long maxFileSize = 5 * 1024 * 1024; // 5 MB

                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    if (file.Length > maxFileSize)
                    {
                        ViewData["ErrorMessage"] = $"File {file.FileName} vượt quá dung lượng cho phép (Max 5MB).";
                        return View();
                    }

                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    {
                        ViewData["ErrorMessage"] = $"File {file.FileName} không đúng định dạng cho phép (.jpg, .png, .pdf, .doc, .docx).";
                        return View();
                    }

                    // Kiểm tra MIME Type để chống giả dạng file
                    var mime = file.ContentType.ToLowerInvariant();
                    if (!allowedMimeTypes.Contains(mime))
                    {
                        ViewData["ErrorMessage"] = $"File {file.FileName} có kiểu nội dung (MIME Type) không hợp lệ.";
                        return View();
                    }

                    // Kiểm tra chữ ký file thực tế (Magic Bytes) để chống đổi đuôi giả mạo (ví dụ .mp4 thành .png)
                    if (!FileHelper.ValidateFileSignature(file, ext))
                    {
                        ViewData["ErrorMessage"] = $"Nội dung file {file.FileName} không trùng khớp với định dạng mở rộng ({ext}) thực tế.";
                        return View();
                    }

                    // Đổi tên file ngẫu nhiên để tránh ghi đè file và tránh Path Traversal
                    var uniqueFileName = $"{Guid.NewGuid()}{ext}";

                    // Tạo thư mục uploads trong wwwroot nếu chưa có
                    var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

                    var filePath = Path.Combine(uploadDir, uniqueFileName);

                    // Lưu file vật lý lên ổ đĩa cứng
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // Thêm vào danh sách đính kèm của Post để lưu thông tin vào DB
                    post.Attachments.Add(new Attachment
                    {
                        FileName = file.FileName, // Tên file hiển thị gốc
                        FilePath = "/uploads/" + uniqueFileName // Đường dẫn tương đối dùng để tải về
                    });
                }
            }

            // Gọi Repository thực hiện lưu Database bằng Transaction
            bool success = _postRepository.CreatePost(post);
            if (success)
            {
                TempData["SuccessMessage"] = "Tạo bài viết mới thành công!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Có lỗi xảy ra khi lưu cơ sở dữ liệu. Toàn bộ thay đổi đã bị hủy (Rollback).");
            return View();
        }

        // 5. POST: Xóa bài viết (Chỉ cho phép Admin hoặc Tác giả xóa bài viết)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("ADMIN");

            // Kiểm tra phân quyền: Chỉ ADMIN hoặc chính chủ mới được xóa
            if (post.UserId.ToString() != userIdClaim && !isAdmin)
            {
                return Forbid();
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

            // Xóa DB
            bool success = _postRepository.DeletePost(id);
            if (success)
            {
                TempData["SuccessMessage"] = "Xóa bài viết thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa bài viết.";
            }

            return RedirectToAction(nameof(Index));
        }



        // 7. GET: Giao diện sửa bài viết
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("ADMIN");

            // Chỉ chủ bài viết hoặc Admin mới được sửa
            if (post.UserId.ToString() != userIdClaim && !isAdmin)
            {
                return Forbid();
            }

            return View(post);
        }

        // 8. POST: Lưu thông tin sửa đổi bài viết (và thêm file đính kèm mới nếu có)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                // Khi lỗi, cần hiển thị lại bài viết và các đính kèm hiện có
                var fullPost = _postRepository.GetPostById(post.Id);
                if (fullPost != null) post.Attachments = fullPost.Attachments;
                return View(post);
            }

            var existingPost = _postRepository.GetPostById(post.Id);
            if (existingPost == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("ADMIN");

            if (existingPost.UserId.ToString() != userIdClaim && !isAdmin)
            {
                return Forbid();
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
            long maxFileSize = 5 * 1024 * 1024; // 5 MB

            // --- BƯỚC 1: XÁC MINH TOÀN BỘ CÁC FILE ĐÍNH KÈM TRƯỚC KHI THỰC HIỆN CẬP NHẬT ---
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    if (file == null || file.Length == 0) continue;

                    // A. Kiểm tra dung lượng
                    if (file.Length > maxFileSize)
                    {
                        ViewData["ErrorMessage"] = $"File {file.FileName} vượt quá dung lượng cho phép (Max 5MB).";
                        post.Attachments = existingPost.Attachments;
                        return View(post);
                    }

                    // B. Kiểm tra phần mở rộng file (Extension)
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    {
                        ViewData["ErrorMessage"] = $"File {file.FileName} không đúng định dạng cho phép (.jpg, .png, .pdf, .doc, .docx).";
                        post.Attachments = existingPost.Attachments;
                        return View(post);
                    }

                    // C. Kiểm tra chữ ký file thực tế (Magic Bytes) để chống đổi đuôi giả mạo
                    if (!FileHelper.ValidateFileSignature(file, ext))
                    {
                        ViewData["ErrorMessage"] = $"Nội dung file {file.FileName} không trùng khớp với định dạng mở rộng ({ext}) thực tế.";
                        post.Attachments = existingPost.Attachments;
                        return View(post);
                    }
                }
            }

            // --- BƯỚC 2: CẬP NHẬT BÀI VIẾT VÀ LƯU FILE KHI ĐÃ HỢP LỆ ---
            bool success = _postRepository.UpdatePost(post);
            if (success)
            {
                if (files != null && files.Count > 0)
                {
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

                TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Cập nhật thất bại. Lỗi CSDL.");
            post.Attachments = existingPost.Attachments;
            return View(post);
        }
    }
}
