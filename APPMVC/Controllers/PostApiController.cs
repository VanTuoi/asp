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

        private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsOwnerOrAdmin(Post p) => p.UserId.ToString() == UserId || User.IsInRole("ADMIN");
        private void DeletePhysicalFile(string path)
        {
            var full = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }

        private static readonly string[] _allowedExt = [".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx"];
        private const long _maxSize = 5 * 1024 * 1024;

        private string? ValidateFiles(List<IFormFile>? files)
        {
            if (files == null) return null;
            foreach (var f in files.Where(f => f?.Length > 0))
            {
                if (f.Length > _maxSize) return $"File {f.FileName} vượt quá 5MB.";
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                if (!_allowedExt.Contains(ext)) return $"File {f.FileName} không đúng định dạng.";
                if (!FileHelper.ValidateFileSignature(f, ext)) return $"Nội dung file {f.FileName} không hợp lệ.";
            }
            return null;
        }

        private async Task PersistFilesAsync(List<IFormFile>? files, Post post, int? postId = null)
        {
            if (files == null) return;
            var dir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(dir);
            foreach (var f in files.Where(f => f?.Length > 0))
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                var name = $"{Guid.NewGuid()}{ext}";
                await using var stream = new FileStream(Path.Combine(dir, name), FileMode.Create);
                await f.CopyToAsync(stream);
                var a = new Attachment { FileName = f.FileName, FilePath = "/uploads/" + name };
                if (postId.HasValue) { a.PostId = postId.Value; _postRepository.AddAttachment(a); }
                else post.Attachments.Add(a);
            }
        }

        [HttpGet, AllowAnonymous]
        public IActionResult GetPosts(string? search) => Json(_postRepository.GetPosts(search));

        [HttpGet, AllowAnonymous]
        public IActionResult GetPost(int id)
        {
            var post = _postRepository.GetPostById(id);
            return post == null
                ? Json(new { success = false, message = "Bài viết không tồn tại." })
                : Json(post);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            if (!int.TryParse(UserId, out int userId)) return Json(new { success = false, message = "Vui lòng đăng nhập lại." });

            post.CreatedAt = DateTime.Now;
            post.UserId = userId;

            var error = ValidateFiles(files);
            if (error != null) return Json(new { success = false, message = error });

            await PersistFilesAsync(files, post);

            return _postRepository.CreatePost(post)
                ? Json(new { success = true, message = "Tạo bài viết thành công!", postId = post.Id })
                : Json(new { success = false, message = "Không thể lưu bài viết vào CSDL." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePost(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid) return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var existing = _postRepository.GetPostById(post.Id);
            if (existing == null) return Json(new { success = false, message = "Bài viết không tồn tại." });
            if (!IsOwnerOrAdmin(existing)) return Json(new { success = false, message = "Bạn không có quyền cập nhật." });

            var error = ValidateFiles(files);
            if (error != null) return Json(new { success = false, message = error });

            if (_postRepository.UpdatePost(post))
            {
                await PersistFilesAsync(files, post, post.Id);
                return Json(new { success = true, message = "Cập nhật bài viết thành công!" });
            }
            return Json(new { success = false, message = "Cập nhật thất bại." });
        }

        [HttpPost]
        public IActionResult DeleteAjax(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null) return Json(new { success = false, message = "Bài viết không tồn tại." });
            if (!IsOwnerOrAdmin(post)) return Json(new { success = false, message = "Bạn không có quyền xóa." });

            foreach (var a in post.Attachments) DeletePhysicalFile(a.FilePath);

            return _postRepository.DeletePost(id)
                ? Json(new { success = true, message = "Xóa bài viết thành công!" })
                : Json(new { success = false, message = "Xóa bài viết thất bại." });
        }

        [HttpPost]
        public IActionResult DeleteAttachmentAjax(int id)
        {
            var attachment = _postRepository.GetAttachmentById(id);
            if (attachment == null) return Json(new { success = false, message = "File đính kèm không tồn tại." });

            var post = _postRepository.GetPostById(attachment.PostId);
            if (post == null) return Json(new { success = false, message = "Bài viết không hợp lệ." });
            if (!IsOwnerOrAdmin(post)) return Json(new { success = false, message = "Bạn không có quyền xóa file này." });

            DeletePhysicalFile(attachment.FilePath);

            return _postRepository.DeleteAttachment(id)
                ? Json(new { success = true, message = "Xóa file thành công!" })
                : Json(new { success = false, message = "Không thể xóa file khỏi CSDL." });
        }
    }
}
