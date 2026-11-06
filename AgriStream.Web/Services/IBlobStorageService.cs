using Microsoft.AspNetCore.Http;

namespace AgriStream.Web.Services;

public interface IBlobStorageService
{
    Task<string> UploadBlobAsync(IFormFile file, string containerName = "drone-media");
    Task<bool> DeleteBlobAsync(string blobUri, string containerName = "drone-media");
}
