using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs;

namespace EduBridge.Services.Settings
{
    public class CenterSettingsService : ICenterSettingsService
    {
        private readonly AppDbContext _context;

        public CenterSettingsService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CenterSettingsDto?> GetSettingsAsync(int ownerId)
        {
            var center = await _context.Centers
                .FirstOrDefaultAsync(c => c.OwnerUserId == ownerId && c.Status == "Active");

            if (center == null)
            {
                return null;
            }

            // Populate fallback values from Center entity to General settings
            var settings = new CenterSettingsDto();
            
            if (!string.IsNullOrEmpty(center.SettingsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<CenterSettingsDto>(center.SettingsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (parsed != null)
                    {
                        settings = parsed;
                    }
                }
                catch
                {
                    // If JSON is invalid, stick to defaults
                }
            }

            // Always override CenterName, Address, Phone, Email with DB master records for UI display fallback if they are empty
            if (string.IsNullOrEmpty(settings.General.CenterName)) settings.General.CenterName = center.CenterName;
            if (string.IsNullOrEmpty(settings.General.Address)) settings.General.Address = center.Address;
            if (string.IsNullOrEmpty(settings.General.PhoneNumber)) settings.General.PhoneNumber = center.PhoneNumber;
            if (string.IsNullOrEmpty(settings.General.Email)) settings.General.Email = center.Email;

            return settings;
        }

        public async Task<bool> UpdateSettingsAsync(int ownerId, CenterSettingsDto settingsDto)
        {
            var center = await _context.Centers
                .FirstOrDefaultAsync(c => c.OwnerUserId == ownerId && c.Status == "Active");

            if (center == null)
            {
                return false;
            }

            // Update master DB fields from General settings
            if (!string.IsNullOrWhiteSpace(settingsDto.General.CenterName))
                center.CenterName = settingsDto.General.CenterName;
            
            center.Address = settingsDto.General.Address;
            center.PhoneNumber = settingsDto.General.PhoneNumber;
            center.Email = settingsDto.General.Email;

            // Serialize settings to JSON
            var json = JsonSerializer.Serialize(settingsDto, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            center.SettingsJson = json;

            _context.Centers.Update(center);
            var result = await _context.SaveChangesAsync();

            return result > 0;
        }

        public async Task<int?> GetOwnerCenterIdAsync(int ownerUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Centers
                .AsNoTracking()
                .Where(center =>
                    center.Status == "Active" &&
                    (center.OwnerUserId == ownerUserId ||
                     _context.CenterUsers.Any(centerUser =>
                         centerUser.CenterId == center.CenterId &&
                         centerUser.UserId == ownerUserId &&
                         centerUser.UserType == "OWNER" &&
                         centerUser.Status == "Active")))
                .Select(c => (int?)c.CenterId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
