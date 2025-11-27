using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace Course_management.Interfaces
{
    public interface ICloudinaryService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file);
        Task<VideoUploadResult> UploadVideoAsync(IFormFile file);
        Task<DeletionResult> DeleteFileAsync(string publicId);
        string GetSecureVideoUrl(string publicId);
    }
}