using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgriStream.Web.Data;
using AgriStream.Web.Models;
using AgriStream.Web.Models.ViewModels;

namespace AgriStream.Web.Controllers;

/// <summary>
/// AccountController manages user authentication, registration, and session management
/// for the AgriStream IoT Smart Farming platform.
/// Integrates with Azure SQL Database to enforce Role-Based Access Control (RBAC).
/// </summary>
public class AccountController : Controller
{
    private readonly AgriStreamDbContext _context;
    private readonly ILogger<AccountController> _logger;

    /// <summary>
    /// Initializes a new instance of the AccountController with EF Core DB Context and logger.
    /// </summary>
    public AccountController(AgriStreamDbContext context, ILogger<AccountController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Displays the user login view.
    /// </summary>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Authenticates user credentials against the Azure SQL Users and Roles tables.
    /// Issues an encrypted cookie identity with claims for RBAC (Admin, Farmer, Agronomist).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            string username = model.Username.Trim();
            string password = model.Password.Trim();

            // Query Azure SQL for matching user record and include their associated Role entity
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => (u.Username == username || u.Username.Trim() == username) && u.Password == password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            // Construct security claims representing the authenticated user's identity and RBAC role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username.Trim()),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Farmer")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            // Sign in the user with ASP.NET Core Cookie Authentication
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
            _logger.LogInformation("User {Username} logged in with role {Role}.", user.Username, user.Role?.RoleName);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database login error occurred.");
            ModelState.AddModelError(string.Empty, $"Database connection issue: {ex.Message}");
            return View(model);
        }
    }

    /// <summary>
    /// Displays the staff registration form populated with system roles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Register()
    {
        await PopulateRolesAsync();
        return View();
    }

    /// <summary>
    /// Registers a new staff account in the Azure SQL Users table.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateRolesAsync();
            return View(model);
        }

        try
        {
            string username = model.Username.Trim();
            bool userExists = await _context.Users.AnyAsync(u => u.Username == username || u.Username.Trim() == username);
            if (userExists)
            {
                ModelState.AddModelError("Username", "Username is already taken.");
                await PopulateRolesAsync();
                return View(model);
            }

            var newUser = new User
            {
                Username = username,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                Password = model.Password.Trim(),
                RoleId = model.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{model.Username}' registered successfully! You can now sign in.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write user to database.");
            ModelState.AddModelError(string.Empty, $"Database write failed: {ex.Message}");
            await PopulateRolesAsync();
            return View(model);
        }
    }

    /// <summary>
    /// Helper method to populate the role dropdown from Azure SQL with a fallback.
    /// </summary>
    private async Task PopulateRolesAsync()
    {
        try
        {
            ViewBag.Roles = await _context.Roles.ToListAsync();
        }
        catch
        {
            ViewBag.Roles = new List<Role>
            {
                new Role { RoleId = 1, RoleName = "Admin", Description = "System Administrator" },
                new Role { RoleId = 2, RoleName = "Farmer", Description = "Farm Manager" },
                new Role { RoleId = 3, RoleName = "Agronomist", Description = "Crop Specialist" }
            };
        }
    }

    /// <summary>
    /// Signs out the current user session and clears authentication cookies.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Displays the Access Denied page when a user attempts an unauthorized action.
    /// </summary>
    public IActionResult AccessDenied()
    {
        return View();
    }
}
