using AgriStream.Web.Models;

namespace AgriStream.Web.Services;

public interface ITableStorageService
{
    Task InsertTelemetryAsync(SensorTelemetryEntity entity);
    Task<List<SensorTelemetryEntity>> GetRecentTelemetryAsync(string deviceSerialNumber, int limit = 10);
    Task<List<SensorTelemetryEntity>> GetAllRecentTelemetryAsync(int limit = 20);
}
