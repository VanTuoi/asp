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

        [AllowAnonymous]
        public IActionResult Index(string? search)
        {
            ViewData["Search"] = search;
            return View(_postRepository.GetPosts(search));
        }

        [AllowAnonymous]
        public IActionResult Details(int id)
        {
            var post = _postRepository.GetPostById(id);
            return post == null ? NotFound() : View(post);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid) return View(post);
            if (!int.TryParse(UserId, out int userId)) return Challenge();

            post.CreatedAt = DateTime.Now;
            post.UserId = userId;

            var error = ValidateFiles(files);
            if (error != null) { ViewData["ErrorMessage"] = error; return View(); }

            await PersistFilesAsync(files, post);

            if (_postRepository.CreatePost(post))
            {
                TempData["SuccessMessage"] = "Tạo bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", "Lỗi lưu CSDL (Rollback).");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null) return NotFound();
            if (!IsOwnerOrAdmin(post)) return Forbid();

            foreach (var a in post.Attachments) DeletePhysicalFile(a.FilePath);

            if (_postRepository.DeletePost(id)) TempData["SuccessMessage"] = "Xóa bài viết thành công!";
            else TempData["ErrorMessage"] = "Không thể xóa bài viết.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var post = _postRepository.GetPostById(id);
            if (post == null) return NotFound();
            if (!IsOwnerOrAdmin(post)) return Forbid();
            return View(post);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Post post, List<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                post.Attachments = _postRepository.GetPostById(post.Id)?.Attachments ?? [];
                return View(post);
            }

            var existing = _postRepository.GetPostById(post.Id);
            if (existing == null) return NotFound();
            if (!IsOwnerOrAdmin(existing)) return Forbid();

            var error = ValidateFiles(files);
            if (error != null) { ViewData["ErrorMessage"] = error; post.Attachments = existing.Attachments; return View(post); }

            if (_postRepository.UpdatePost(post))
            {
                await PersistFilesAsync(files, post, post.Id);
                TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError("", "Cập nhật thất bại.");
            post.Attachments = existing.Attachments;
            return View(post);
        }
    }
}
