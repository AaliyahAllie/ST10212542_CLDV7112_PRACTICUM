using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions.Functions;

public class TriggerIrrigationPost
{
    private readonly ILogger<TriggerIrrigationPost> _logger;

    public TriggerIrrigationPost(ILogger<TriggerIrrigationPost> logger)
    {
        _logger = logger;
    }

    [Function("TriggerIrrigationPost")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "irrigation/trigger")] HttpRequest req)
    {
        _logger.LogInformation("[HTTP POST] Received command to trigger irrigation.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(requestBody))
        {
            return new BadRequestObjectResult(new { error = "Request body cannot be empty. Expects JSON with deviceSerialNumber and durationMinutes." });
        }

        string? storageConnection = Environment.GetEnvironmentVariable("AzureStorageConnection");
        if (!string.IsNullOrEmpty(storageConnection) && !storageConnection.Contains("<your-storage"))
        {
            try
            {
                var queueClient = new QueueClient(storageConnection, "irrigation-commands");
                await queueClient.CreateIfNotExistsAsync();

                var bytes = Encoding.UTF8.GetBytes(requestBody);
                await queueClient.SendMessageAsync(Convert.ToBase64String(bytes));

                _logger.LogInformation("[HTTP POST] Enqueued irrigation command to 'irrigation-commands' queue: {Body}", requestBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HTTP POST] Failed to place message into Azure Storage Queue.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        return new OkObjectResult(new
        {
            success = true,
            status = "Queued",
            message = "Irrigation command enqueued into 'irrigation-commands' queue.",
            timestamp = DateTime.UtcNow
        });
    }
}
