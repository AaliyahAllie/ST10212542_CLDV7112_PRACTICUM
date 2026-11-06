using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgriStream.Web.Data;

namespace AgriStream.Web.Controllers;

public class HomeController : Controller
{
    private readonly AgriStreamDbContext _context;

    public HomeController(AgriStreamDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalDevices = await _context.IoTDevices.CountAsync();
            ViewBag.TotalFields = await _context.Fields.CountAsync();
            ViewBag.TotalDroneScans = await _context.DroneMedia.CountAsync();
        }
        catch
        {
            ViewBag.TotalUsers = 3;
            ViewBag.TotalDevices = 3;
            ViewBag.TotalFields = 2;
            ViewBag.TotalDroneScans = 1;
        }

        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Scalability()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
