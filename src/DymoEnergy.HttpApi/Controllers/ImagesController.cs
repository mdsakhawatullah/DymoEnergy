using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DymoEnergy.FileUploads;

namespace DymoEnergy.Controllers;

[ApiController]
[Route("api/app/[controller]")]
public class ImagesController : DymoEnergyController
{
    private readonly ICloudinaryService _cloudinaryService;

    public ImagesController(ICloudinaryService cloudinaryService)
    {
        _cloudinaryService = cloudinaryService;
    }

    /// <summary>
    /// Uploads an image to Cloudinary
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <param name="folder">Optional folder name (default: DymoEnergy)</param>
    /// <returns>Uploaded image URL</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, string folder = "DymoEnergy")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        try
        {
            using var memoryStream = new System.IO.MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var imageUrl = await _cloudinaryService.UploadImageAsync(memoryStream, file.FileName, folder);
            return Ok(new { url = imageUrl });
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an image from Cloudinary
    /// </summary>
    /// <param name="publicId">The public ID of the image</param>
    [HttpDelete("{publicId}")]
    public async Task<IActionResult> Delete(string publicId)
    {
        if (string.IsNullOrEmpty(publicId))
            return BadRequest(new { message = "PublicId is required" });

        try
        {
            await _cloudinaryService.DeleteImageAsync(publicId);
            return Ok(new { message = "Image deleted successfully" });
        }
        catch (System.Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
