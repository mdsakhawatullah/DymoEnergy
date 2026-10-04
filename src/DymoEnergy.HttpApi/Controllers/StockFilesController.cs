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

/// <summary>Invoice, challan and photo uploads for stock entries.</summary>
[ApiController]
[Route("api/app/stock-files")]
[Authorize(DymoEnergyPermissions.Stock.Edit)]
public class StockFilesController : DymoEnergyController
{
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly string[] Allowed = { ".pdf", ".jpg", ".jpeg", ".png" };

    private readonly ICloudinaryService _cloudinary;

    public StockFilesController(ICloudinaryService cloudinary)
    {
        _cloudinary = cloudinary;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = "Files can be at most 10 MB." });
        if (!Allowed.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            return BadRequest(new { message = "Only PDF, JPG or PNG files." });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var url = await _cloudinary.UploadDocumentAsync(ms, file.FileName, "DymoEnergy/stock");
            return Ok(new { url, fileName = file.FileName, sizeBytes = file.Length });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
