namespace AgriStream.Web.Models.ViewModels;

public class DashboardViewModel
{
    public int TotalFarms { get; set; }
    public int TotalFields { get; set; }
    public int TotalOnlineDevices { get; set; }
    public int TotalDroneScans { get; set; }

    public List<Farm> Farms { get; set; } = new();
    public List<Field> Fields { get; set; } = new();
    public List<IoTDevice> Devices { get; set; } = new();
    public List<SensorTelemetryEntity> RecentTelemetry { get; set; } = new();
    public string SelectedDeviceId { get; set; } = string.Empty;
}
