using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions.Functions;

public class GetLatestTelemetry
{
    private readonly ILogger<GetLatestTelemetry> _logger;

    public GetLatestTelemetry(ILogger<GetLatestTelemetry> logger)
    {
        _logger = logger;
    }

    [Function("GetLatestTelemetry")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "telemetry")] HttpRequest req)
    {
        string? deviceId = req.Query["deviceId"];
        _logger.LogInformation("[HTTP GET] Fetching latest telemetry readings. Device parameter: {DeviceId}", deviceId ?? "ALL");

        string? storageConnection = Environment.GetEnvironmentVariable("AzureStorageConnection");
        var results = new List<TableEntity>();

        if (!string.IsNullOrEmpty(storageConnection) && !storageConnection.Contains("<your-storage"))
        {
            try
            {
                var tableClient = new TableClient(storageConnection, "SensorTelemetry");
                await tableClient.CreateIfNotExistsAsync();

                string? filter = string.IsNullOrEmpty(deviceId) ? null : $"PartitionKey eq '{deviceId}'";
                var query = tableClient.QueryAsync<TableEntity>(filter: filter, maxPerPage: 10);

                await foreach (var page in query.AsPages())
                {
                    results.AddRange(page.Values);
                    if (results.Count >= 10) break;
                }

                return new OkObjectResult(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HTTP GET] Error querying Azure Table Storage.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        // Return simulated mock readings if Azure storage connection is not yet configured
        return new OkObjectResult(new[]
        {
            new { DeviceId = deviceId ?? "SN-SOIL-001", SoilMoisture = 24.5, Temperature = 21.8, Battery = 95, Status = "Optimal", Timestamp = DateTime.UtcNow },
            new { DeviceId = deviceId ?? "SN-SOIL-001", SoilMoisture = 23.9, Temperature = 22.3, Battery = 94, Status = "Optimal", Timestamp = DateTime.UtcNow.AddMinutes(-5) }
        });
    }
}
