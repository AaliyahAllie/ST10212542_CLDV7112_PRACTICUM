namespace AgriStream.Web.Models;

public class IoTDevice
{
    public int DeviceId { get; set; }
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public int FieldId { get; set; }
    public string Status { get; set; } = "Online";
    public int? BatteryLevelPercentage { get; set; }
    public DateTime InstallationDate { get; set; } = DateTime.UtcNow;

    public Field? Field { get; set; }
}
