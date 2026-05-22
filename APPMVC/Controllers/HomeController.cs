using System;
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
        private readonly DocumentRepositories _documentRepository;
        private readonly IWebHostEnvironment _env;

        public HomeController(DocumentRepositories docRepo, IWebHostEnvironment env)
        {
            _documentRepository = docRepo;
            _env = env;
        }

        private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsOwnerOrAdmin(Document doc) => doc.UserId.ToString() == UserId || User.IsInRole("ADMIN");

        private void DeleteFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var fullPath = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }

        private string? ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > 5 * 1024 * 1024) return "Dung lượng file vượt quá giới hạn 5MB.";
            
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" }.Contains(ext)) 
                return "Định dạng file không được hỗ trợ.";
            
            if (!FileHelper.ValidateFileSignature(file, ext)) 
                return "Nội dung file không hợp lệ (nghi ngờ giả mạo đuôi file).";
            
            return null;
        }

        private async Task<(string FilePath, string FileName)> SaveFileAsync(IFormFile file)
        {
            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
            var fullPath = Path.Combine(_env.WebRootPath, "uploads", uniqueName);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return ("/uploads/" + uniqueName, file.FileName);
        }

        [HttpGet, AllowAnonymous]
        public IActionResult GetDocumentsJson(string? search) => Json(_documentRepository.GetDocuments(search));

        [AllowAnonymous]
        public IActionResult Index(string? search)
        {
            ViewData["Search"] = search;
            return View(_documentRepository.GetDocuments(search));
        }

        [AllowAnonymous]
        public IActionResult Details(int id)
        {
            var doc = _documentRepository.GetDocumentById(id);
            return doc == null ? NotFound() : View(doc);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentViewModel model, IFormFile? file)
        {
            if (file == null || file.Length == 0) ModelState.AddModelError("file", "Vui lòng chọn file.");
            if (!ModelState.IsValid) return View(model);

            var err = ValidateFile(file);
            if (err != null) { ViewData["ErrorMessage"] = err; return View(model); }

            var (filePath, fileName) = await SaveFileAsync(file!);
            var doc = new Document
            {
                Title = model.Title,
                Description = model.Description,
                FilePath = filePath,
                FileName = fileName,
                UploadedAt = DateTime.Now,
                UserId = int.Parse(UserId!)
            };

            if (_documentRepository.CreateDocument(doc))
            {
                TempData["SuccessMessage"] = "Thêm tài liệu thành công!";
                return RedirectToAction(nameof(Index));
            }

            DeleteFile(filePath);
            ModelState.AddModelError("", "Lưu thông tin thất bại.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var doc = _documentRepository.GetDocumentById(id);
            if (doc == null) return NotFound();
            if (!IsOwnerOrAdmin(doc)) return Forbid();

            return View(new DocumentViewModel
            {
                Id = doc.Id,
                Title = doc.Title,
                Description = doc.Description,
                CurrentFileName = doc.FileName,
                CurrentFilePath = doc.FilePath
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DocumentViewModel model, IFormFile? file)
        {
            var doc = _documentRepository.GetDocumentById(model.Id);
            if (doc == null) return NotFound();
            if (!IsOwnerOrAdmin(doc)) return Forbid();

            if (!ModelState.IsValid)
            {
                model.CurrentFileName = doc.FileName;
                model.CurrentFilePath = doc.FilePath;
                return View(model);
            }

            string? newPath = null, newName = null;
            if (file != null && file.Length > 0)
            {
                var err = ValidateFile(file);
                if (err != null) { ViewData["ErrorMessage"] = err; return View(model); }
                (newPath, newName) = await SaveFileAsync(file);
            }

            var toUpdate = new Document
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                FilePath = newPath ?? string.Empty,
                FileName = newName ?? string.Empty
            };

            if (_documentRepository.UpdateDocument(toUpdate))
            {
                if (!string.IsNullOrEmpty(newPath)) DeleteFile(doc.FilePath);
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(newPath)) DeleteFile(newPath);
            ModelState.AddModelError("", "Cập nhật thất bại.");
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var doc = _documentRepository.GetDocumentById(id);
            if (doc == null) return NotFound();
            if (!IsOwnerOrAdmin(doc)) return Forbid();

            DeleteFile(doc.FilePath);
            if (_documentRepository.DeleteDocument(id)) TempData["SuccessMessage"] = "Xóa thành công!";
            else TempData["ErrorMessage"] = "Xóa thất bại.";

            return RedirectToAction(nameof(Index));
        }
    }
}
