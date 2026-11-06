namespace AgriStream.Web.Models.ViewModels;

public class IrrigationCommandDto
{
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public int FieldId { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string TriggeredBy { get; set; } = "System";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
