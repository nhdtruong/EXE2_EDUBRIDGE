using EduBridge.Contracts.Classes;
using EduBridge.DTOs.Centers;
using System.Threading;
using System.Threading.Tasks;

namespace EduBridge.Services.SystemAdmin;

public interface ISystemAdminCenterService
{
    Task<ApiResponse<CenterDto>> CreateCenterAsync(CreateCenterRequestDto request, int currentUserId, CancellationToken cancellationToken = default);
}
