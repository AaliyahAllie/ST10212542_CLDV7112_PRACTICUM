using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using AgriStream.Web.Models.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Web.Services;

public class QueueStorageService : IQueueStorageService
{
    private readonly QueueClient? _queueClient;
    private readonly ILogger<QueueStorageService> _logger;

    public QueueStorageService(IConfiguration configuration, ILogger<QueueStorageService> logger)
    {
        _logger = logger;
        string? connectionString = configuration.GetConnectionString("AzureStorageConnection");

        if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("<your-storage"))
        {
            try
            {
                _queueClient = new QueueClient(connectionString, "irrigation-commands");
                _queueClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize Azure QueueClient. Operating in test mode.");
            }
        }
    }

    public async Task<bool> EnqueueIrrigationCommandAsync(IrrigationCommandDto command)
    {
        string json = JsonSerializer.Serialize(command);
        _logger.LogInformation("Enqueuing irrigation command for {DeviceSerialNumber}: {Json}", command.DeviceSerialNumber, json);

        if (_queueClient != null)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _queueClient.SendMessageAsync(Convert.ToBase64String(bytes));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to Azure Storage Queue.");
                return false;
            }
        }

        return true; // simulated success for offline development
    }
}
