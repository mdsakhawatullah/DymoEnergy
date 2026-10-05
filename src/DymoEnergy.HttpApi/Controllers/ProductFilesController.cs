using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DymoEnergy.FileUploads;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DymoEnergy.Controllers;

/// <summary>PDF uploads (datasheets, manuals) for the product Download tab.</summary>
[ApiController]
[Route("api/app/product-files")]
[Authorize(DymoEnergyPermissions.Products.Default)]
public class ProductFilesController : DymoEnergyController
{
    private const long MaxBytes = 15 * 1024 * 1024;

    private readonly ICloudinaryService _cloudinary;

    public ProductFilesController(ICloudinaryService cloudinary)
    {
        _cloudinary = cloudinary;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = "The PDF can be at most 15 MB." });
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only PDF files." });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var url = await _cloudinary.UploadDocumentAsync(ms, file.FileName, "DymoEnergy/products");
            return Ok(new { url, fileName = file.FileName, sizeBytes = file.Length });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
