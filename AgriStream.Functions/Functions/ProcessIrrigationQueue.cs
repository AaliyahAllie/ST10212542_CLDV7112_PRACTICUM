using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions.Functions;

public class ProcessIrrigationQueue
{
    private readonly ILogger<ProcessIrrigationQueue> _logger;

    public ProcessIrrigationQueue(ILogger<ProcessIrrigationQueue> logger)
    {
        _logger = logger;
    }

    [Function("ProcessIrrigationQueue")]
    public async Task Run(
        [QueueTrigger("irrigation-commands", Connection = "AzureStorageConnection")] string messageText,
        FunctionContext context)
    {
        _logger.LogInformation("[QueueTrigger] Received message from 'irrigation-commands' queue: {Message}", messageText);

        try
        {
            var command = JsonSerializer.Deserialize<IrrigationQueueMessage>(messageText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (command == null || string.IsNullOrEmpty(command.DeviceSerialNumber))
            {
                _logger.LogWarning("[QueueTrigger] Invalid or empty command payload.");
                return;
            }

            _logger.LogInformation("[QueueTrigger] Executing irrigation for device: {SerialNumber}, Duration: {Minutes} mins",
                command.DeviceSerialNumber, command.DurationMinutes);

            string? sqlConnection = Environment.GetEnvironmentVariable("SqlConnectionString");
            if (!string.IsNullOrEmpty(sqlConnection) && !sqlConnection.Contains("<your-server"))
            {
                using var conn = new SqlConnection(sqlConnection);
                await conn.OpenAsync();

                string updateQuery = @"
                    UPDATE IoTDevices 
                    SET Status = 'Active - Irrigating' 
                    WHERE DeviceSerialNumber = @SerialNumber;";

                using var cmd = new SqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@SerialNumber", command.DeviceSerialNumber);

                int affected = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation("[QueueTrigger] Successfully updated {Count} row(s) in Azure SQL IoTDevices table.", affected);
            }
            else
            {
                _logger.LogWarning("[QueueTrigger] SqlConnectionString not configured. Simulated status update for {SerialNumber}.", command.DeviceSerialNumber);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QueueTrigger] Failed to process message from queue.");
            throw; // Retries according to Azure Queue poison-message policy
        }
    }
}

public class IrrigationQueueMessage
{
    public string DeviceSerialNumber { get; set; } = string.Empty;
    public int FieldId { get; set; }
    public int DurationMinutes { get; set; }
    public string? TriggeredBy { get; set; }
}
