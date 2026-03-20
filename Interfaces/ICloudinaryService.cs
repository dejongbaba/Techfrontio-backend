using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace Course_management.Interfaces
{
    public interface ICloudinaryService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file, bool transform = true);
        Task<VideoUploadResult> UploadVideoAsync(IFormFile file);
        Task<UploadResult> UploadDocumentAsync(IFormFile file);
        Task<DeletionResult> DeleteFileAsync(string publicId);
        string GetSecureVideoUrl(string publicId);
        string GetSecureUrl(string publicId, string resourceType);
    }
}