namespace AgriStream.Web.Models;

public class DroneMedia
{
    public int MediaId { get; set; }
    public int FieldId { get; set; }
    public int UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public int? FlightAltitudeMeters { get; set; }
    public string? ImageResolution { get; set; }
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Field? Field { get; set; }
    public User? UploadedByUser { get; set; }
}
