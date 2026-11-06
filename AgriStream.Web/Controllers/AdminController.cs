using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgriStream.Web.Data;

namespace AgriStream.Web.Controllers;

/// <summary>
/// AdminController manages administrative personnel governance, system roles,
/// and user account deletion in the Azure SQL Database.
/// Exclusively accessible to users authenticated with the 'Admin' role (RBAC).
/// </summary>
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AgriStreamDbContext _context;
    private readonly ILogger<AdminController> _logger;

    public AdminController(AgriStreamDbContext context, ILogger<AdminController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Displays the master staff directory with total counts by role.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.UserId)
                .ToListAsync();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalAdmins = users.Count(u => u.Role?.RoleName == "Admin");
            ViewBag.TotalFarmers = users.Count(u => u.Role?.RoleName == "Farmer");
            ViewBag.TotalAgronomists = users.Count(u => u.Role?.RoleName == "Agronomist");

            return View(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load staff list from Azure SQL.");
            TempData["ErrorMessage"] = "Could not load staff list: " + ex.Message;
            return View(new List<Models.User>());
        }
    }

    /// <summary>
    /// Deletes a user record from Azure SQL while preventing self-deletion.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Username.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "You cannot delete your own active Admin account.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Staff member '{user.Username}' was successfully removed from the system.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user #{UserId}.", id);
            TempData["ErrorMessage"] = "Could not delete user: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
