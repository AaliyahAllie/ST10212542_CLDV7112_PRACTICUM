using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriStream.Web.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient? _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger;
        string? connectionString = configuration.GetConnectionString("AzureStorageConnection");

        if (!string.IsNullOrEmpty(connectionString))
        {
            try
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure BlobServiceClient.");
            }
        }
    }

    public async Task<string> UploadBlobAsync(IFormFile file, string containerName = "drone-media")
    {
        if (_blobServiceClient == null)
        {
            throw new InvalidOperationException("Azure Storage connection is not configured in appsettings.json.");
        }

        string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            // Safe creation: does not demand public container access which Azure blocks by default
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(uniqueFileName);
            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            _logger.LogInformation("Successfully uploaded {FileName} to Azure Blob Storage.", uniqueFileName);

            // Generate a secure read SAS token so image renders properly even if anonymous public access is disabled
            if (blobClient.CanGenerateSasUri)
            {
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddYears(1));
                return sasUri.ToString();
            }

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Blob Storage upload failed for {FileName}.", file.FileName);
            throw new Exception($"Failed to upload to Azure Blob Storage: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteBlobAsync(string blobUri, string containerName = "drone-media")
    {
        if (_blobServiceClient != null && Uri.TryCreate(blobUri, UriKind.Absolute, out var uri))
        {
            try
            {
                string blobName = Path.GetFileName(uri.LocalPath);
                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                var response = await blobClient.DeleteIfExistsAsync();
                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete blob from Azure Blob Storage.");
            }
        }
        return false;
    }
}
