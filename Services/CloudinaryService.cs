using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Course_management.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace Course_management.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration configuration;

        public CloudinaryService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file)
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
            };
            return await _cloudinary.UploadAsync(uploadParams);
        }

        public async Task<VideoUploadResult> UploadVideoAsync(IFormFile file)
        {
            var uploadParams = new VideoUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream()),
                EagerAsync = true
            };
            return await _cloudinary.UploadAsync(uploadParams);
        }

        public async Task<DeletionResult> DeleteFileAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deleteParams);
        }

        public string GetSecureVideoUrl(string publicId)
        {
            var toSign = publicId;
            var apiSecret = configuration["Cloudinary:ApiKey"];
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(toSign + apiSecret));
            var signature = Convert.ToBase64String(hash).Substring(0, 8).Replace("+", "-").Replace("/", "_");
            var baseUrl = _cloudinary.Api.UrlVideoUp.BuildUrl(publicId);
            // Assuming the baseUrl is like https://res.cloudinary.com/cloud/video/publicId
            // Insert s--signature-- after /video/\n
            var signedUrl = baseUrl.Replace("/video/", $"/video/s--{signature}--/");
            return signedUrl;
        }
    }
}