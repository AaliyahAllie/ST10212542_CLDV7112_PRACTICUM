using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions.Functions;

public class ProcessDroneImageBlob
{
    private readonly ILogger<ProcessDroneImageBlob> _logger;

    public ProcessDroneImageBlob(ILogger<ProcessDroneImageBlob> logger)
    {
        _logger = logger;
    }

    [Function("ProcessDroneImageBlob")]
    public async Task Run(
        [BlobTrigger("drone-media/{name}", Connection = "AzureStorageConnection")] Stream blobStream,
        string name,
        FunctionContext context)
    {
        _logger.LogInformation("[BlobTrigger] New drone aerial image detected in container 'drone-media': {FileName}, Size: {Size} bytes", name, blobStream.Length);

        string? storageConnection = Environment.GetEnvironmentVariable("AzureStorageConnection");
        if (!string.IsNullOrEmpty(storageConnection) && !storageConnection.Contains("<your-storage"))
        {
            try
            {
                // Write audit entry directly into Azure Table Storage
                var tableClient = new TableClient(storageConnection, "DroneProcessingAudit");
                await tableClient.CreateIfNotExistsAsync();

                var auditEntry = new TableEntity("DroneUploadAudit", Guid.NewGuid().ToString())
                {
                    { "FileName", name },
                    { "FileSizeBytes", blobStream.Length },
                    { "ProcessingStatus", "Completed - Ready for NDVI Analysis" },
                    { "AuditTimestamp", DateTime.UtcNow }
                };

                await tableClient.AddEntityAsync(auditEntry);
                _logger.LogInformation("[BlobTrigger] Successfully wrote processing audit entry for {FileName} into Table Storage.", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BlobTrigger] Error writing audit log to Azure Table Storage.");
            }
        }
        else
        {
            _logger.LogWarning("[BlobTrigger] AzureStorageConnection not configured. Processed simulated blob {FileName}.", name);
        }
    }
}
