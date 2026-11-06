using AgriStream.Web.Models.ViewModels;

namespace AgriStream.Web.Services;

public interface IQueueStorageService
{
    Task<bool> EnqueueIrrigationCommandAsync(IrrigationCommandDto command);
}
