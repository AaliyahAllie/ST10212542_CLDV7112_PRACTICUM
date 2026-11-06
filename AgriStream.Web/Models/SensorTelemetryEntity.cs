using Azure;
using Azure.Data.Tables;

namespace AgriStream.Web.Models;

public class SensorTelemetryEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // DeviceSerialNumber
    public string RowKey { get; set; } = string.Empty;       // Inverted Timestamp for chronological sorting
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public double SoilMoisturePercentage { get; set; }
    public double TemperatureCelsius { get; set; }
    public int BatteryLevel { get; set; }
    public string Status { get; set; } = "Normal";

    public SensorTelemetryEntity() { }

    public SensorTelemetryEntity(string deviceSerialNumber, double moisture, double temp, int battery = 95, string status = "Normal")
    {
        PartitionKey = deviceSerialNumber;
        RowKey = string.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks);
        SoilMoisturePercentage = moisture;
        TemperatureCelsius = temp;
        BatteryLevel = battery;
        Status = status;
    }
}
