using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace APPMVC.Helpers
{
    public static class FileHelper
    {
        private static readonly Dictionary<string, byte[]> FileSignatures = new Dictionary<string, byte[]>
        {
            { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            { ".doc", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
            { ".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } }
        };

        // Hàm kiểm tra chữ ký của file thực tế (Magic Bytes) để tránh đổi đuôi file giả mạo
        public static bool ValidateFileSignature(IFormFile file, string ext)
        {
            if (file == null || string.IsNullOrEmpty(ext)) return false;
            ext = ext.ToLowerInvariant();
            if (!FileSignatures.ContainsKey(ext)) return false;

            var expectedSignature = FileSignatures[ext];
            using (var stream = file.OpenReadStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var headerBytes = reader.ReadBytes(expectedSignature.Length);
                    if (headerBytes.Length < expectedSignature.Length) return false;

                    if (ext == ".jpg" || ext == ".jpeg")
                    {
                        return headerBytes.Take(3).SequenceEqual(expectedSignature);
                    }
                    return headerBytes.SequenceEqual(expectedSignature);
                }
            }
        }
    }
}
