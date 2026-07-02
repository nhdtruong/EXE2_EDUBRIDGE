using EduBridge.Services.Classes;
using EduBridge.Contracts.Courses;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using EduBridge.Services.Auth;

namespace EduBridge.Services.Courses;

public class CourseManagementService : ICourseManagementService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CourseManagementService> _logger;
    private readonly ICurrentCenterService _currentCenterService;

    public CourseManagementService(AppDbContext context, ILogger<CourseManagementService> logger, ICurrentCenterService currentCenterService)
    {
        _context = context;
        _logger = logger;
        _currentCenterService = currentCenterService;
    }

    private async Task<int?> GetOwnerCenterIdAsync(int ownerUserId, CancellationToken cancellationToken) =>
        await _currentCenterService.GetCenterIdAsync(cancellationToken);

    public async Task<ClassOperationResult<CoursePagedResponse>> GetCoursesAsync(int ownerUserId, CourseQuery query, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<CoursePagedResponse>.Failure("Không tìm thấy trung tâm đang hoạt động.");

        var dbQuery = _context.Courses
            .AsNoTracking()
            .Where(c => c.CenterId == centerId.Value && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var search = query.Keyword.Trim();
            dbQuery = dbQuery.Where(c => c.CourseCode.Contains(search) || c.CourseName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            dbQuery = dbQuery.Where(c => c.Status == query.Status);
        }

        var totalItems = await dbQuery.CountAsync(cancellationToken);

        var courses = await dbQuery
            .OrderByDescending(c => c.CourseId)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CourseResponse
            {
                CourseId = c.CourseId,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                Description = c.Description,
                TotalSessions = c.TotalSessions,
                TuitionFee = c.TuitionFee,
                Status = c.Status,
                ClassCount = c.Classes.Count
            })
            .ToListAsync(cancellationToken);

        return ClassOperationResult<CoursePagedResponse>.Success(new CoursePagedResponse
        {
            Data = courses,
            TotalItems = totalItems,
            Page = query.PageNumber,
            PageSize = query.PageSize
        }, "Success");
    }

    public async Task<ClassOperationResult<CourseResponse>> GetCourseAsync(int ownerUserId, int courseId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<CourseResponse>.Failure("Không tìm thấy trung tâm đang hoạt động.");

        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.CourseId == courseId && c.CenterId == centerId.Value && !c.IsDeleted)
            .Select(c => new CourseResponse
            {
                CourseId = c.CourseId,
                CourseCode = c.CourseCode,
                CourseName = c.CourseName,
                Description = c.Description,
                TotalSessions = c.TotalSessions,
                TuitionFee = c.TuitionFee,
                Status = c.Status,
                ClassCount = c.Classes.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null) return ClassOperationResult<CourseResponse>.Failure("Không tìm thấy môn học.");

        return ClassOperationResult<CourseResponse>.Success(course, "Success");
    }

    public async Task<ClassOperationResult<CourseMutationResponse>> CreateCourseAsync(int ownerUserId, SaveCourseRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<CourseMutationResponse>.Failure("Không tìm thấy trung tâm đang hoạt động.");

        var codeExists = await _context.Courses.AnyAsync(c => c.CenterId == centerId.Value && !c.IsDeleted && c.CourseCode == request.CourseCode, cancellationToken);
        if (codeExists) return ClassOperationResult<CourseMutationResponse>.Failure("Mã môn đã tồn tại trong trung tâm.");

        var nameExists = await _context.Courses.AnyAsync(c => c.CenterId == centerId.Value && !c.IsDeleted && c.CourseName == request.CourseName, cancellationToken);
        if (nameExists) return ClassOperationResult<CourseMutationResponse>.Failure("Tên môn đã tồn tại trong trung tâm.");

        var course = new Course
        {
            CenterId = centerId.Value,
            CourseCode = request.CourseCode,
            CourseName = request.CourseName,
            Description = request.Description,
            TotalSessions = request.TotalSessions ?? 24,
            TuitionFee = request.TuitionFee,
            Status = request.Status
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<CourseMutationResponse>.Success(
            new CourseMutationResponse(course.CourseId, course.CourseCode, course.Status), 
            "Thêm môn học thành công."
        );
    }

    public async Task<ClassOperationResult<CourseMutationResponse>> UpdateCourseAsync(int ownerUserId, int courseId, SaveCourseRequest request, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<CourseMutationResponse>.Failure("Không tìm thấy trung tâm đang hoạt động.");

        var course = await _context.Courses.Include(c => c.Classes)
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.CenterId == centerId.Value && !c.IsDeleted, cancellationToken);

        if (course == null) return ClassOperationResult<CourseMutationResponse>.Failure("Không tìm thấy môn học cần cập nhật.");

        var codeExists = await _context.Courses.AnyAsync(c => c.CenterId == centerId.Value && !c.IsDeleted && c.CourseId != courseId && c.CourseCode == request.CourseCode, cancellationToken);
        if (codeExists) return ClassOperationResult<CourseMutationResponse>.Failure("Mã môn đã tồn tại trong trung tâm.");

        var nameExists = await _context.Courses.AnyAsync(c => c.CenterId == centerId.Value && !c.IsDeleted && c.CourseId != courseId && c.CourseName == request.CourseName, cancellationToken);
        if (nameExists) return ClassOperationResult<CourseMutationResponse>.Failure("Tên môn đã tồn tại trong trung tâm.");

        if (request.Status == "Inactive" && course.Classes.Any(c => c.Status == "Active"))
        {
            return ClassOperationResult<CourseMutationResponse>.Failure("Không thể tạm dừng môn học khi vẫn còn lớp đang hoạt động.");
        }

        course.CourseCode = request.CourseCode;
        course.CourseName = request.CourseName;
        course.Description = request.Description;
        course.TotalSessions = request.TotalSessions ?? 24;
        course.TuitionFee = request.TuitionFee;
        course.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<CourseMutationResponse>.Success(
            new CourseMutationResponse(course.CourseId, course.CourseCode, course.Status), 
            "Cập nhật môn học thành công."
        );
    }

    public async Task<ClassOperationResult<CourseMutationResponse>> SetStatusAsync(int ownerUserId, int courseId, string status, CancellationToken cancellationToken = default)
    {
        if (status is not ("Active" or "Inactive")) return ClassOperationResult<CourseMutationResponse>.Failure("Trạng thái không hợp lệ.");
        
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<CourseMutationResponse>.Failure("Không tìm thấy trung tâm đang hoạt động.");

        var course = await _context.Courses.Include(c => c.Classes)
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.CenterId == centerId.Value && !c.IsDeleted, cancellationToken);

        if (course == null) return ClassOperationResult<CourseMutationResponse>.Failure("Không tìm thấy môn học cần đổi trạng thái.");

        if (status == "Inactive" && course.Classes.Any(c => c.Status == "Active"))
        {
            return ClassOperationResult<CourseMutationResponse>.Failure("Không thể tạm dừng môn học khi vẫn còn lớp đang hoạt động.");
        }

        course.Status = status;

        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<CourseMutationResponse>.Success(
            new CourseMutationResponse(courseId, course.CourseCode, status), 
            "Đổi trạng thái môn học thành công."
        );
    }

    public async Task<ClassOperationResult<bool>> DeleteCourseAsync(int ownerUserId, int courseId, CancellationToken cancellationToken = default)
    {
        var centerId = await GetOwnerCenterIdAsync(ownerUserId, cancellationToken);
        if (centerId == null) return ClassOperationResult<bool>.Failure("Không tìm thấy trung tâm đang hoạt động.");

        var course = await _context.Courses.Include(c => c.Classes)
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.CenterId == centerId.Value && !c.IsDeleted, cancellationToken);

        if (course == null) return ClassOperationResult<bool>.Failure("Không tìm thấy môn học cần xóa.");

        if (!string.Equals(course.Status, "Inactive", StringComparison.OrdinalIgnoreCase))
        {
            return ClassOperationResult<bool>.Failure("Chỉ được xóa môn học đang tạm dừng.");
        }

        if (course.Classes.Any(c => c.Status == "Active"))
        {
            return ClassOperationResult<bool>.Failure("Không thể xóa môn học khi vẫn còn lớp đang hoạt động.");
        }

        course.IsDeleted = true;
        course.DeletedAt = DateTime.UtcNow;
        course.DeletedByUserId = ownerUserId;
        course.Status = "Inactive";

        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<bool>.Success(true, "Xóa môn học thành công.");
    }
}
