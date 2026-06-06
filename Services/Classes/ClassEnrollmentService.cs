using System.Data;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Classes;

public sealed class ClassEnrollmentService : IClassEnrollmentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClassEnrollmentService> _logger;

    public ClassEnrollmentService(AppDbContext context, ILogger<ClassEnrollmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ClassOperationResult<IReadOnlyList<EnrolledStudentResponse>>> GetEnrolledStudentsAsync(
        int ownerUserId, int classId, CancellationToken cancellationToken = default)
    {
        if (!await CanManageClassAsync(ownerUserId, classId, cancellationToken))
            return Failure<IReadOnlyList<EnrolledStudentResponse>>("Không tìm thấy lớp học hoặc bạn không có quyền truy cập.");

        var students = await _context.Enrollments.AsNoTracking()
            .Where(e => e.ClassId == classId && e.Status != "Đã nghỉ" && !e.Student.IsDeleted)
            .OrderBy(e => e.Status == "Đang học" ? 0 : 1)
            .ThenBy(e => e.Student.FullName)
            .Select(e => new EnrolledStudentResponse(
                e.EnrollmentId, e.StudentId, e.Student.StudentCode, e.Student.FullName,
                e.Student.AvatarUrl, e.EnrollDate, e.Status))
            .ToListAsync(cancellationToken);

        return ClassOperationResult<IReadOnlyList<EnrolledStudentResponse>>.Success(students, "Tải danh sách học sinh thành công.");
    }

    public async Task<ClassOperationResult<IReadOnlyList<AvailableStudentResponse>>> GetAvailableStudentsAsync(
        int ownerUserId, int classId, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var classInfo = await ManagedClasses(ownerUserId)
            .Where(c => c.ClassId == classId)
            .Select(c => new { c.CenterId, c.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (classInfo == null)
            return Failure<IReadOnlyList<AvailableStudentResponse>>("Không tìm thấy lớp học hoặc bạn không có quyền truy cập.");

        var query = _context.Students.AsNoTracking()
            .Where(s =>
                s.CenterId == classInfo.CenterId &&
                s.Status == "Active" &&
                !s.IsDeleted &&
                !s.Enrollments.Any(e => e.ClassId == classId && e.Status != "Đã nghỉ"));

        keyword = keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(s => s.StudentCode.Contains(keyword) || s.FullName.Contains(keyword));

        var students = await query.OrderBy(s => s.FullName).ThenBy(s => s.StudentCode).Take(50)
            .Select(s => new AvailableStudentResponse(s.StudentId, s.StudentCode, s.FullName, s.AvatarUrl, s.PhoneNumber))
            .ToListAsync(cancellationToken);

        return ClassOperationResult<IReadOnlyList<AvailableStudentResponse>>.Success(students, "Tải danh sách học sinh khả dụng thành công.");
    }

    public async Task<ClassOperationResult<EnrollStudentsResponse>> EnrollStudentsAsync(
        int ownerUserId, int classId, EnrollStudentRequest request, CancellationToken cancellationToken = default)
    {
        var studentIds = request.StudentIds?.Where(id => id > 0).Distinct().ToList() ?? [];
        if (studentIds.Count == 0)
            return Failure<EnrollStudentsResponse>("Vui lòng chọn ít nhất một học sinh.", "StudentIds");
        if (studentIds.Count > 100)
            return Failure<EnrollStudentsResponse>("Mỗi lần chỉ được thêm tối đa 100 học sinh.", "StudentIds");

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var cls = await ManagedClasses(ownerUserId).FirstOrDefaultAsync(c => c.ClassId == classId, cancellationToken);
            if (cls == null) return Failure<EnrollStudentsResponse>("Không tìm thấy lớp học hoặc bạn không có quyền thao tác.");
            if (cls.Status != "Active") return Failure<EnrollStudentsResponse>("Chỉ có thể thêm học sinh vào lớp đang hoạt động.", "ClassId");

            var students = await _context.Students
                .Where(s => studentIds.Contains(s.StudentId) && s.CenterId == cls.CenterId && s.Status == "Active" && !s.IsDeleted)
                .ToListAsync(cancellationToken);
            if (students.Count != studentIds.Count)
                return Failure<EnrollStudentsResponse>("Có học sinh không hợp lệ, đã ngưng hoạt động hoặc không thuộc trung tâm.", "StudentIds");

            var existing = await _context.Enrollments
                .Where(e => e.ClassId == classId && studentIds.Contains(e.StudentId))
                .ToDictionaryAsync(e => e.StudentId, cancellationToken);
            var now = GetVietnamNow();
            var today = DateOnly.FromDateTime(now);
            var added = 0;
            var reactivated = 0;
            var result = new List<Enrollment>();

            foreach (var student in students)
            {
                if (existing.TryGetValue(student.StudentId, out var enrollment))
                {
                    if (enrollment.Status != "Đã nghỉ")
                        return Failure<EnrollStudentsResponse>($"{student.FullName} hiện đã thuộc lớp.", "StudentIds");

                    enrollment.EnrollDate = today;
                    enrollment.Status = "Đang học";
                    enrollment.StatusChangedAt = now;
                    enrollment.UpdatedByUserId = ownerUserId;
                    enrollment.Note = NormalizeNote(request.Note);
                    AddHistory(enrollment, "Đã nghỉ", "Đang học", ownerUserId, now, request.Note);
                    reactivated++;
                    result.Add(enrollment);
                    continue;
                }

                var newEnrollment = new Enrollment
                {
                    ClassId = classId,
                    StudentId = student.StudentId,
                    EnrollDate = today,
                    Status = "Đang học",
                    StatusChangedAt = now,
                    UpdatedByUserId = ownerUserId,
                    Note = NormalizeNote(request.Note)
                };
                AddHistory(newEnrollment, null, "Đang học", ownerUserId, now, request.Note);
                _context.Enrollments.Add(newEnrollment);
                added++;
                result.Add(newEnrollment);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = result.Join(students, e => e.StudentId, s => s.StudentId,
                (e, s) => new EnrolledStudentResponse(e.EnrollmentId, s.StudentId, s.StudentCode, s.FullName, s.AvatarUrl, e.EnrollDate, e.Status)).ToList();
            return ClassOperationResult<EnrollStudentsResponse>.Success(
                new EnrollStudentsResponse(added, reactivated, response),
                $"Đã thêm {response.Count} học sinh vào lớp.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Could not enroll students into class {ClassId}.", classId);
            return Failure<EnrollStudentsResponse>("Không thể thêm học sinh do dữ liệu vừa thay đổi. Vui lòng thử lại.");
        }
    }

    public async Task<ClassOperationResult<bool>> RemoveStudentAsync(
        int ownerUserId, int classId, int studentId, string? note = null, CancellationToken cancellationToken = default)
    {
        var enrollment = await _context.Enrollments
            .Include(e => e.Class)
            .FirstOrDefaultAsync(e => e.ClassId == classId && e.StudentId == studentId, cancellationToken);
        if (enrollment == null || !await CanManageClassAsync(ownerUserId, classId, cancellationToken))
            return Failure<bool>("Không tìm thấy học sinh trong lớp hoặc bạn không có quyền thao tác.");
        if (enrollment.Class.Status != "Active" || enrollment.Class.IsDeleted)
            return Failure<bool>("Chỉ có thể gỡ học sinh khỏi lớp đang hoạt động.");
        if (enrollment.Status == "Đã nghỉ")
            return Failure<bool>("Học sinh đã được gỡ khỏi lớp trước đó.");

        var oldStatus = enrollment.Status;
        var now = GetVietnamNow();
        enrollment.Status = "Đã nghỉ";
        enrollment.StatusChangedAt = now;
        enrollment.UpdatedByUserId = ownerUserId;
        enrollment.Note = NormalizeNote(note);
        AddHistory(enrollment, oldStatus, "Đã nghỉ", ownerUserId, now, note);
        await _context.SaveChangesAsync(cancellationToken);
        return ClassOperationResult<bool>.Success(true, "Đã gỡ học sinh khỏi lớp và giữ nguyên lịch sử.");
    }

    private IQueryable<Class> ManagedClasses(int ownerUserId) =>
        _context.Classes.Where(c =>
            !c.IsDeleted &&
            (c.Center.OwnerUserId == ownerUserId ||
             _context.CenterUsers.Any(cu => cu.CenterId == c.CenterId && cu.UserId == ownerUserId && cu.UserType == "OWNER" && cu.Status == "Active")));

    private Task<bool> CanManageClassAsync(int ownerUserId, int classId, CancellationToken cancellationToken) =>
        ManagedClasses(ownerUserId).AnyAsync(c => c.ClassId == classId, cancellationToken);

    private static void AddHistory(Enrollment enrollment, string? oldStatus, string newStatus, int userId, DateTime changedAt, string? note) =>
        enrollment.EnrollmentHistories.Add(new EnrollmentHistory
        {
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = changedAt,
            ChangedByUserId = userId,
            Note = NormalizeNote(note)
        });

    private static string? NormalizeNote(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ClassOperationResult<T> Failure<T>(string message, string key = "") =>
        ClassOperationResult<T>.Failure(message, new Dictionary<string, string[]> { [key] = [message] });
    private static DateTime GetVietnamNow()
    {
        try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); }
        catch { return DateTime.UtcNow.AddHours(7); }
    }
}
