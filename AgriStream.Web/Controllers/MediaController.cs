using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AgriStream.Web.Data;
using AgriStream.Web.Models;
using AgriStream.Web.Models.ViewModels;
using AgriStream.Web.Services;

namespace AgriStream.Web.Controllers;

/// <summary>
/// MediaController manages high-resolution aerial drone survey uploads, gallery browsing,
/// and metadata persistence for the AgriStream IoT Smart Farming platform.
/// Integrates with Azure Blob Storage (media files) and Azure SQL Database (relational metadata).
/// </summary>
[Authorize]
public class MediaController : Controller
{
    private readonly AgriStreamDbContext _context;
    private readonly IBlobStorageService _blobService;
    private readonly ILogger<MediaController> _logger;

    /// <summary>
    /// Injects database context, blob storage service, and logger.
    /// </summary>
    public MediaController(AgriStreamDbContext context, IBlobStorageService blobService, ILogger<MediaController> logger)
    {
        _context = context;
        _blobService = blobService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the gallery of drone aerial scans retrieved from Azure SQL and Blob Storage.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var mediaList = await _context.DroneMedia
                .Include(m => m.Field)
                .Include(m => m.UploadedByUser)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();

            return View(mediaList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load drone surveys from Azure SQL.");
            TempData["ErrorMessage"] = "Could not load drone surveys from database: " + ex.Message;
            return View(new List<DroneMedia>());
        }
    }

    /// <summary>
    /// Displays the drone image upload form.
    /// Restricted to Agronomist and Admin roles.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Agronomist,Admin")]
    public async Task<IActionResult> Upload()
    {
        await PopulateFieldsDropdownAsync();
        return View(new UploadDroneMediaViewModel());
    }

    /// <summary>
    /// Uploads a drone flight image directly to Azure Blob Storage ('drone-media' container)
    /// and persists relational flight metadata in Azure SQL.
    /// Restricted to Agronomist and Admin roles.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Agronomist,Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(UploadDroneMediaViewModel model)
    {
        if (model.ImageFile == null || model.ImageFile.Length == 0)
        {
            ModelState.AddModelError("ImageFile", "Please choose a drone image file to upload.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateFieldsDropdownAsync();
            return View(model);
        }

        try
        {
            // 1. Upload the image directly to Azure Blob Storage container 'drone-media'
            string blobUrl = await _blobService.UploadBlobAsync(model.ImageFile!, "drone-media");

            // 2. Determine logged-in user ID from authentication claims
            string? userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.TryParse(userIdStr, out int parsedId) ? parsedId : 1;

            // 3. Save relational metadata directly to Azure SQL DroneMedia table
            var droneMedia = new DroneMedia
            {
                FieldId = model.FieldId,
                UploadedByUserId = userId,
                FileName = model.ImageFile!.FileName,
                BlobUrl = blobUrl,
                FlightAltitudeMeters = model.FlightAltitudeMeters,
                ImageResolution = "3840x2160 (4K)",
                CapturedAt = DateTime.UtcNow,
                UploadedAt = DateTime.UtcNow
            };

            _context.DroneMedia.Add(droneMedia);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Drone scan '{model.ImageFile.FileName}' successfully saved to Azure Blob Storage!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload drone image.");
            ModelState.AddModelError(string.Empty, $"Upload failed: {ex.Message}");
            await PopulateFieldsDropdownAsync();
            return View(model);
        }
    }

    /// <summary>
    /// Deletes a drone survey record from Azure SQL and purges the blob from Azure Blob Storage.
    /// Restricted to Admin role.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var media = await _context.DroneMedia.FindAsync(id);
            if (media != null)
            {
                // Delete binary asset from Azure Blob Storage
                await _blobService.DeleteBlobAsync(media.BlobUrl, "drone-media");

                // Remove relational database record
                _context.DroneMedia.Remove(media);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Drone scan removed successfully.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete drone survey #{MediaId}.", id);
            TempData["ErrorMessage"] = "Could not delete drone scan: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Populates the field parcel dropdown from Azure SQL with a fallback.
    /// </summary>
    private async Task PopulateFieldsDropdownAsync()
    {
        try
        {
            var fields = await _context.Fields.ToListAsync();
            ViewBag.Fields = new SelectList(fields, "FieldId", "FieldName");
        }
        catch
        {
            ViewBag.Fields = new SelectList(new[]
            {
                new { FieldId = 1, FieldName = "North Parcel Alpha (Maize)" },
                new { FieldId = 2, FieldName = "South Parcel Beta (Soybeans)" }
            }, "FieldId", "FieldName");
        }
    }
}
