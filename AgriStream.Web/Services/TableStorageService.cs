using Azure.Data.Tables;
using AgriStream.Web.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Web.Services;

public class TableStorageService : ITableStorageService
{
    private readonly TableClient? _tableClient;
    private readonly ILogger<TableStorageService> _logger;
    private static readonly List<SensorTelemetryEntity> _localCache = new();

    public TableStorageService(IConfiguration configuration, ILogger<TableStorageService> logger)
    {
        _logger = logger;
        string? connectionString = configuration.GetConnectionString("AzureStorageConnection");

        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("<your-storage"))
        {
            try
            {
                _tableClient = new TableClient(connectionString, "SensorTelemetry");
                _tableClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize Azure TableClient. Using simulated storage cache.");
            }
        }

        // Initialize sample telemetry if local cache is empty
        if (!_localCache.Any())
        {
            _localCache.AddRange(new[]
            {
                new SensorTelemetryEntity("SN-SOIL-001", 24.8, 22.4, 94, "Optimal"),
                new SensorTelemetryEntity("SN-SOIL-001", 23.9, 23.1, 93, "Optimal"),
                new SensorTelemetryEntity("SN-WTH-002",  12.0, 26.5, 98, "High Solar Radiation"),
                new SensorTelemetryEntity("SN-SOIL-003", 17.2, 21.0, 88, "Low Moisture Alert")
            });
        }
    }

    public async Task InsertTelemetryAsync(SensorTelemetryEntity entity)
    {
        _localCache.Insert(0, entity);

        if (_tableClient != null)
        {
            try
            {
                await _tableClient.AddEntityAsync(entity);
                _logger.LogInformation("Saved telemetry record for {DeviceId} to Azure Table Storage.", entity.PartitionKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to Azure Table Storage.");
            }
        }
    }

    public async Task<List<SensorTelemetryEntity>> GetRecentTelemetryAsync(string deviceSerialNumber, int limit = 10)
    {
        if (_tableClient != null)
        {
            try
            {
                var results = new List<SensorTelemetryEntity>();
                var query = _tableClient.QueryAsync<SensorTelemetryEntity>(
                    filter: $"PartitionKey eq '{deviceSerialNumber}'",
                    maxPerPage: limit);

                await foreach (var page in query.AsPages())
                {
                    results.AddRange(page.Values);
                    if (results.Count >= limit) break;
                }

                if (results.Any()) return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query Azure Table Storage.");
            }
        }

        // Return from local cache
        return _localCache
            .Where(x => string.Equals(x.PartitionKey, deviceSerialNumber, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
    }

    public async Task<List<SensorTelemetryEntity>> GetAllRecentTelemetryAsync(int limit = 20)
    {
        if (_tableClient != null)
        {
            try
            {
                var results = new List<SensorTelemetryEntity>();
                var query = _tableClient.QueryAsync<SensorTelemetryEntity>(maxPerPage: limit);

                await foreach (var page in query.AsPages())
                {
                    results.AddRange(page.Values);
                    if (results.Count >= limit) break;
                }

                if (results.Any()) return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query all records from Azure Table Storage.");
            }
        }

        return _localCache.Take(limit).ToList();
    }
}
