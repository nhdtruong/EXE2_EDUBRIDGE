using EduBridge.Contracts.Users;
using EduBridge.Models;
using EduBridge.Services.Classes;
using System.Threading;
using System.Threading.Tasks;

namespace EduBridge.Services.Users;

public interface IUserProfileService
{
    Task<ClassOperationResult<UserProfileResponse>> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ClassOperationResult<bool>> UpdateAvatarAsync(int userId, System.IO.Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
}