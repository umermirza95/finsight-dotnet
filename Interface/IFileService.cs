using Finsight.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Finsight.Interfaces
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IBrowserFile file, string userId);
        Task<FSFile?> GetFileByIdAsync(Guid fileId);
        Task DeleteFileAsync(Guid fileId);
    }   
}