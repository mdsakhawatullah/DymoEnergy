using System;
using System.IO;
using System.Threading.Tasks;
using DymoEnergy.FileUploads;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DymoEnergy.Controllers;

/// <summary>Video uploads (brand hero clips, factory tours) handed straight to Cloudinary.</summary>
[ApiController]
[Route("api/app/videos")]
public class VideosController : DymoEnergyController
{
    private const long MaxBytes = 50 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = { ".mp4", ".webm", ".mov", ".m4v", ".ogv" };

    private readonly ICloudinaryService _cloudinaryService;

    public VideosController(ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Uploads a video to Cloudinary and returns its public URL.
    /// </summary>
    /// <param name="file">The video file to upload</param>
    /// <param name="folder">Optional folder name (default: DymoEnergy/videos)</param>
    [HttpPost("upload")]
    [RequestSizeLimit(MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes)]
    public async Task<IActionResult> Upload(IFormFile file, string folder = "DymoEnergy/videos")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = "The video can be at most 50 MB." });

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (Array.IndexOf(AllowedExtensions, extension) < 0)
            return BadRequest(new { message = $"Only {string.Join(", ", AllowedExtensions)} videos." });

        try
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var url = await _cloudinaryService.UploadVideoAsync(memoryStream, file.FileName, folder);
            return Ok(new { url, fileName = file.FileName, sizeBytes = file.Length });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
