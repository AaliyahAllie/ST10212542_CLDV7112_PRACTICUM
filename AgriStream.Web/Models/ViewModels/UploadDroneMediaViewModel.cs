using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace AgriStream.Web.Models.ViewModels;

public class UploadDroneMediaViewModel
{
    [Required]
    [Display(Name = "Select Field")]
    public int FieldId { get; set; }

    [Display(Name = "Flight Altitude (meters)")]
    [Range(10, 500, ErrorMessage = "Flight altitude must be between 10m and 500m.")]
    public int FlightAltitudeMeters { get; set; } = 120;

    [Required]
    [Display(Name = "Drone Aerial Image File")]
    public IFormFile? ImageFile { get; set; }
}
