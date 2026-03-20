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
            this.configuration = configuration;
            if (this.configuration == null) Console.WriteLine("CRITICAL: Configuration is null in CloudinaryService constructor!");
            else Console.WriteLine("CloudinaryService constructor called, configuration injected.");
            
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, bool transform = true)
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream())
            };

            if (transform)
            {
                uploadParams.Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face");
            }
            
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

        public async Task<UploadResult> UploadDocumentAsync(IFormFile file)
        {
            // Note: Use ImageUploadParams if we want to treat PDF as image for preview generation
            // But Raw is safer for pure document storage. However, frontend expects image/pdf stream.
            // Let's stick to ImageUpload for PDFs to get pages, but without crop.
            
            if (file.ContentType == "application/pdf")
            {
                 var imgParams = new ImageUploadParams()
                 {
                     File = new FileDescription(file.FileName, file.OpenReadStream())
                 };
                 return await _cloudinary.UploadAsync(imgParams);
            }

            var uploadParams = new RawUploadParams()
            {
                File = new FileDescription(file.FileName, file.OpenReadStream())
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
            var apiSecret = configuration["Cloudinary:ApiSecret"];
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(toSign + apiSecret));
            var signature = Convert.ToBase64String(hash).Substring(0, 8).Replace("+", "-").Replace("/", "_");
            var baseUrl = _cloudinary.Api.UrlVideoUp.BuildUrl(publicId);
            // Assuming the baseUrl is like https://res.cloudinary.com/cloud/video/publicId
            // Insert s--signature-- after /video/\n
            var signedUrl = baseUrl.Replace("/video/", $"/video/s--{signature}--/");
            return signedUrl;
        }

        public string GetSecureUrl(string publicId, string resourceType)
        {
            var toSign = publicId;
            var apiSecret = configuration["Cloudinary:ApiSecret"]; // Fix: Use ApiSecret, not ApiKey for signing!
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(toSign + apiSecret));
            var signature = Convert.ToBase64String(hash).Substring(0, 8).Replace("+", "-").Replace("/", "_");
            
            string baseUrl;
            if (resourceType == "video")
            {
                baseUrl = _cloudinary.Api.UrlVideoUp.BuildUrl(publicId);
                return baseUrl.Replace("/video/", $"/video/s--{signature}--/");
            }
            else if (resourceType == "image" || resourceType == "pdf") // PDFs are often treated as images
            {
                baseUrl = _cloudinary.Api.UrlImgUp.BuildUrl(publicId);
                return baseUrl.Replace("/image/", $"/image/s--{signature}--/");
            }
            else // raw
            {
                // For raw files, use manual construction as SDK might not expose UrlRawUp easily or consistent
                // Actually, Cloudinary .NET SDK usually handles this if we use Url.Resource(resourceType)
                // But sticking to the pattern:
                var cloudName = configuration["Cloudinary:CloudName"];
                baseUrl = $"https://res.cloudinary.com/{cloudName}/raw/upload/v1/{publicId}";
                return baseUrl.Replace("/raw/", $"/raw/s--{signature}--/");
            }
        }
    }
}