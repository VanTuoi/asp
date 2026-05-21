using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public HomeController(DocumentRepositories documentRepository, IWebHostEnvironment env)
        {
            _documentRepository = documentRepository;
            _env = env;
        }

        private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsOwnerOrAdmin(Document doc) => doc.UserId.ToString() == UserId || User.IsInRole("ADMIN");

        private void DeletePhysicalFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var fullPath = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
            if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
        }

        private static readonly string[] _allowedExt = [".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx"];
        private const long _maxSize = 5 * 1024 * 1024; // 5MB

        private string? ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            if (file.Length > _maxSize) return "Dung lượng file vượt quá giới hạn 5MB.";
            
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExt.Contains(ext)) return "Định dạng file không được hỗ trợ (chỉ nhận JPG, PNG, PDF, DOC, DOCX).";
            
            if (!FileHelper.ValidateFileSignature(file, ext)) return "Nội dung file không hợp lệ (nghi ngờ giả mạo phần mở rộng).";
            
            return null;
        }

        private async Task<(string FilePath, string FileName)> SavePhysicalFileAsync(IFormFile file)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsDir, uniqueName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return ("/uploads/" + uniqueName, file.FileName);
        }

        [HttpGet, AllowAnonymous]
        public IActionResult GetDocumentsJson(string? search)
        {
            var docs = _documentRepository.GetDocuments(search);
            return Json(docs);
        }

        [AllowAnonymous]
        public IActionResult Index(string? search, int? categoryId)
        {
            ViewData["Search"] = search;
            ViewData["CategoryId"] = categoryId;
            
            ViewBag.Categories = _documentRepository.GetCategories()
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name, Selected = c.Id == categoryId })
                .ToList();

            var docs = _documentRepository.GetDocuments(search, categoryId);
            return View(docs);
        }

        [AllowAnonymous]
        public IActionResult Details(int id)
        {
            var doc = _documentRepository.GetDocumentById(id);
            if (doc == null) return NotFound();
            return View(doc);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_documentRepository.GetCategories(), "Id", "Name");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DocumentViewModel model, IFormFile? file)
        {
            ViewBag.Categories = new SelectList(_documentRepository.GetCategories(), "Id", "Name", model.CategoryId);
            
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Vui lòng chọn file đính kèm.");
            }

            if (!ModelState.IsValid) return View(model);

            var fileError = ValidateFile(file);
            if (fileError != null)
            {
                ViewData["ErrorMessage"] = fileError;
                return View(model);
            }

            if (!int.TryParse(UserId, out int userId)) return Challenge();

            var (filePath, fileName) = await SavePhysicalFileAsync(file!);

            var doc = new Document
            {
                Title = model.Title,
                Description = model.Description,
                FilePath = filePath,
                FileName = fileName,
                UploadedAt = DateTime.Now,
                CategoryId = model.CategoryId,
                UserId = userId
            };

            if (_documentRepository.CreateDocument(doc))
            {
                TempData["SuccessMessage"] = "Thêm tài liệu thành công!";
                return RedirectToAction(nameof(Index));
            }

            DeletePhysicalFile(filePath);
            ModelState.AddModelError("", "Lưu thông tin vào cơ sở dữ liệu thất bại.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var doc = _documentRepository.GetDocumentById(id);
            if (doc == null) return NotFound();
            if (!IsOwnerOrAdmin(doc)) return Forbid();

            var model = new DocumentViewModel
            {
                Id = doc.Id,
                Title = doc.Title,
                Description = doc.Description,
                CategoryId = doc.CategoryId,
                CurrentFileName = doc.FileName,
                CurrentFilePath = doc.FilePath
            };

            ViewBag.Categories = new SelectList(_documentRepository.GetCategories(), "Id", "Name", doc.CategoryId);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DocumentViewModel model, IFormFile? file)
        {
            var existingDoc = _documentRepository.GetDocumentById(model.Id);
            if (existingDoc == null) return NotFound();
            if (!IsOwnerOrAdmin(existingDoc)) return Forbid();

            ViewBag.Categories = new SelectList(_documentRepository.GetCategories(), "Id", "Name", model.CategoryId);

            if (!ModelState.IsValid)
            {
                model.CurrentFileName = existingDoc.FileName;
                model.CurrentFilePath = existingDoc.FilePath;
                return View(model);
            }

            string? newFilePath = null;
            string? newFileName = null;

            if (file != null && file.Length > 0)
            {
                var fileError = ValidateFile(file);
                if (fileError != null)
                {
                    ViewData["ErrorMessage"] = fileError;
                    model.CurrentFileName = existingDoc.FileName;
                    model.CurrentFilePath = existingDoc.FilePath;
                    return View(model);
                }

                (newFilePath, newFileName) = await SavePhysicalFileAsync(file);
            }

            var docToUpdate = new Document
            {
                Id = model.Id,
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                FilePath = newFilePath ?? string.Empty,
                FileName = newFileName ?? string.Empty
            };

            if (_documentRepository.UpdateDocument(docToUpdate))
            {
                if (!string.IsNullOrEmpty(newFilePath))
                {
                    DeletePhysicalFile(existingDoc.FilePath);
                }
                TempData["SuccessMessage"] = "Cập nhật tài liệu thành công!";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(newFilePath))
            {
                DeletePhysicalFile(newFilePath);
            }

            ModelState.AddModelError("", "Cập nhật tài liệu thất bại.");
            model.CurrentFileName = existingDoc.FileName;
            model.CurrentFilePath = existingDoc.FilePath;
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var doc = _documentRepository.GetDocumentById(id);
            if (doc == null) return NotFound();
            if (!IsOwnerOrAdmin(doc)) return Forbid();

            DeletePhysicalFile(doc.FilePath);

            if (_documentRepository.DeleteDocument(id))
            {
                TempData["SuccessMessage"] = "Xóa tài liệu thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Xóa tài liệu thất bại.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
