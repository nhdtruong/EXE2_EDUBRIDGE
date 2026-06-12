using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EduBridge.Services.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public LocalFileStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folderName, CancellationToken cancellationToken = default)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream is empty.");

            if (!ValidateFileSignature(fileStream, fileName))
                throw new ArgumentException("Định dạng file không hợp lệ hoặc chứa nội dung độc hại (Sai Magic Bytes).");

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(stream, cancellationToken);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty.");

            using var stream = file.OpenReadStream();
            return await SaveFileAsync(stream, file.FileName, folderName, cancellationToken);
        }

        public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return Task.CompletedTask;

            // Extract relative path
            var relativePath = fileUrl.TrimStart('/');
            var absolutePath = Path.Combine(_environment.WebRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            return Task.CompletedTask;
        }

        private static readonly Dictionary<string, List<byte[]>> _fileSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
            { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
            { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
            { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
            { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".pptx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".zip", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".rar", new List<byte[]> { new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 } } }
        };

        private bool ValidateFileSignature(Stream fileStream, string fileName)
        {
            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext)) return false;

            if (fileStream.Length < 8)
                return false;

            using var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8, leaveOpen: true);
            var headerBytes = reader.ReadBytes(8);
            fileStream.Position = 0; // Reset position for downstream reading

            // 1. Block executable magic bytes 'MZ' (4D 5A) completely
            if (headerBytes[0] == 0x4D && headerBytes[1] == 0x5A)
            {
                return false;
            }

            // 2. Enforce strict magic bytes check for known extensions
            if (_fileSignatures.TryGetValue(ext, out var signatures))
            {
                return signatures.Any(signature =>
                    headerBytes.Take(signature.Length).SequenceEqual(signature)
                );
            }

            // If the extension is not in our known list, but it's not an executable (MZ), we allow it.
            // E.g., .txt or .csv files which don't have reliable magic bytes.
            return true;
        }
    }
}
