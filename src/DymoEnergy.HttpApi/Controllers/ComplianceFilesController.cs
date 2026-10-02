using System;
using System.Threading.Tasks;
using DymoEnergy.Compliance;
using DymoEnergy.FileUploads;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DymoEnergy.Controllers;

/// <summary>Document upload and the "download everything" archive for the compliance page.</summary>
[ApiController]
[Route("api/app/compliance-files")]
[Authorize(DymoEnergyPermissions.Compliance.Default)]
public class ComplianceFilesController : DymoEnergyController
{
    private const long MaxBytes = 15 * 1024 * 1024;

    private readonly ICloudinaryService _cloudinary;
    private readonly IComplianceArchiveService _archive;

    public ComplianceFilesController(ICloudinaryService cloudinary, IComplianceArchiveService archive)
    {
        _cloudinary = cloudinary;
        _archive = archive;
    }

    [HttpPost("upload")]
    [Authorize(DymoEnergyPermissions.Compliance.Create)]
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
            var url = await _cloudinary.UploadDocumentAsync(ms, file.FileName);
            return Ok(new { url, fileName = file.FileName, sizeBytes = file.Length });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("download-all")]
    public async Task<IActionResult> DownloadAll()
    {
        var stream = await _archive.BuildArchiveAsync();
        if (stream == null)
            return NotFound(new { message = "There are no uploaded documents yet." });

        return File(stream, "application/zip", $"compliance-documents-{DateTime.UtcNow:yyyyMMdd}.zip");
    }
}
