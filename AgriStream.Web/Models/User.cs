namespace AgriStream.Web.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Role? Role { get; set; }
    public ICollection<Farm> ManagedFarms { get; set; } = new List<Farm>();
    public ICollection<DroneMedia> UploadedMedia { get; set; } = new List<DroneMedia>();
}
