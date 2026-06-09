using System.Threading.Tasks;
using EduBridge.Models.DTOs;

namespace EduBridge.Services.Settings
{
    public interface ICenterSettingsService
    {
        Task<CenterSettingsDto> GetSettingsAsync(int ownerId);
        Task<bool> UpdateSettingsAsync(int ownerId, CenterSettingsDto settingsDto);
    }
}
