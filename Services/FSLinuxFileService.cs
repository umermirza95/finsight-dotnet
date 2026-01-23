using Finsight.Interfaces;
using Finsight.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Finsight.Services
{
    public class FSLinuxFile(AppDbContext context) : IFileService
    {
        private readonly string _basePath = "/var/www/finsight/uploads";
         private readonly AppDbContext _context = context;
        public Task DeleteFileAsync(Guid fileId)
        {
            throw new NotImplementedException();
        }

        public Task<FSFile?> GetFileByIdAsync(Guid fileId)
        {
            throw new NotImplementedException();
        }

        public async Task<string> UploadFileAsync(IBrowserFile file, string userId)
        {
            var userFolder = Path.Combine(_basePath, userId);
            if (!Directory.Exists(userFolder)) Directory.CreateDirectory(userFolder);
            var trustedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
            var fullPath = Path.Combine(userFolder, trustedFileName);
            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 10).CopyToAsync(stream);
            return fullPath;
        }
    }
}