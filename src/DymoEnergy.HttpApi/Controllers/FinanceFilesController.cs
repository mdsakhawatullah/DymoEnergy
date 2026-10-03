using System;
using System.Threading.Tasks;
using DymoEnergy.FileUploads;
using DymoEnergy.Finance;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DymoEnergy.Controllers;

/// <summary>Receipt upload and the accountant export for the Financials page.</summary>
[ApiController]
[Route("api/app/finance-files")]
[Authorize(DymoEnergyPermissions.Finance.Default)]
public class FinanceFilesController : DymoEnergyController
{
    private const long MaxBytes = 15 * 1024 * 1024;

    private readonly ICloudinaryService _cloudinary;
    private readonly IFinanceExportService _export;

    public FinanceFilesController(ICloudinaryService cloudinary, IFinanceExportService export)
    {
        _cloudinary = cloudinary;
        _export = export;
    }

    [HttpPost("upload")]
    [Authorize(DymoEnergyPermissions.Finance.Create)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });
        if (file.Length > MaxBytes)
            return BadRequest(new { message = "Files can be at most 15 MB." });

        try
        {
            using var ms = new System.IO.MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;
            var url = await _cloudinary.UploadDocumentAsync(ms, file.FileName, "DymoEnergy/finance");
            return Ok(new { url, fileName = file.FileName, sizeBytes = file.Length });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? period)
    {
        var (stream, fileName) = await _export.BuildAsync(period);
        return File(stream, "application/zip", fileName);
    }
}
