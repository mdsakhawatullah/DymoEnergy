using System;
using System.IO;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using DymoEnergy.FileUploads;

namespace DymoEnergy.Application.FileUploads;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            throw new InvalidOperationException("Cloudinary configuration is missing");

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(System.IO.Stream fileStream, string fileName, string folder = "DymoEnergy")
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new InvalidOperationException("File is empty");

        if (string.IsNullOrEmpty(fileName))
            throw new InvalidOperationException("FileName is required");

        ValidateFile(fileName);

        var uploadParams = new ImageUploadParams()
        {
            File = new FileDescription(fileName, fileStream),
            Folder = folder,
            PublicId = $"{Guid.NewGuid()}"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new InvalidOperationException($"Upload failed: {uploadResult.Error.Message}");

        return uploadResult.SecureUrl.ToString();
    }

    public async Task DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrEmpty(publicId))
            throw new InvalidOperationException("PublicId is required");

        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        if (result.Error != null)
            throw new InvalidOperationException($"Delete failed: {result.Error.Message}");
    }

    private void ValidateFile(string fileName)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();

        if (Array.IndexOf(allowedExtensions, fileExtension) < 0)
            throw new InvalidOperationException($"File type {fileExtension} is not allowed. Allowed types: jpg, jpeg, png, gif, webp");
    }
}
