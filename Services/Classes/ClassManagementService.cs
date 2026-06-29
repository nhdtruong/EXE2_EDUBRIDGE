using System.Data;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;
using EduBridge.Services.Auth;

namespace EduBridge.Services.Classes;

public sealed class ClassManagementService : IClassManagementService
{
    private readonly AppDbContext _context;
    private readonly IClassCreationService _creationService;
    private readonly IClassLessonPlanner _lessonPlanner;
    private readonly ILogger<ClassManagementService> _logger;
    private readonly ICurrentCenterService _currentCenterService;

    public ClassManagementService(
        AppDbContext context,
        IClassCreationService creationService,
        IClassLessonPlanner lessonPlanner,
        ILogger<ClassManagementService> logger,
        ICurrentCenterService currentCenterService)
    {
        _context = context;
        _creationService = creationService;
        _lessonPlanner = lessonPlanner;
        _logger = logger;
        _currentCenterService = currentCenterService;
    }

    public async Task<ClassOperationResult<ClassEditResponse>> GetEditAsync(
        int ownerUserId,
        int classId,
        CancellationToken cancellationToken = default)
    {
        var managedClasses = await ManagedClassesAsync(ownerUserId, cancellationToken);
        var entity = await managedClasses
            .AsNoTracking()
            .Include(c => c.ClassSchedules)
            .FirstOrDefaultAsync(c => c.ClassId == classId, cancellationToken);

        if (entity == null)
        {
            return Failure<ClassEditResponse>("Không tìm thấy lớp học hoặc bạn không có quyền quản lý.", "ClassId");
        }

        if (entity.Status == "Closed")
        {
            return Failure<ClassEditResponse>("Lớp đã đóng không thể cập nhật.", "Status");
        }

        var optionsResult = await _creationService.GetCreateOptionsAsync(
            ownerUserId,
            entity.CenterId,
            cancellationToken);

        if (!optionsResult.IsSuccess || optionsResult.Value == null)
        {
            return ClassOperationResult<ClassEditResponse>.Failure(optionsResult.Message, optionsResult.Errors);
        }

        var schedules = entity.ClassSchedules
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(s => new ClassScheduleRequest
            {
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            })
            .ToList();

        var options = await IncludeCurrentOptionsAsync(entity, optionsResult.Value, cancellationToken);

        return ClassOperationResult<ClassEditResponse>.Success(
            new ClassEditResponse(
                entity.ClassId,
                entity.CenterId,
                entity.ClassCode,
                entity.ClassName,
                entity.CourseId,
                entity.TeacherId,
                entity.RoomId,
                entity.StartDate,
                entity.EndDate,
                entity.TotalSessions,
                entity.Status,
                Convert.ToBase64String(entity.RowVersion),
                schedules,
                options),
            "Tải thông tin lớp học thành công.");
    }

    public async Task<ClassOperationResult<ClassMutationResponse>> UpdateAsync(
        int ownerUserId,
        UpdateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        Normalize(request);
        var errors = await ValidateAsync(ownerUserId, request, cancellationToken);

        if (errors.Count > 0)
        {
            return ClassOperationResult<ClassMutationResponse>.Failure(
                "Không thể cập nhật lớp học. Vui lòng kiểm tra lại dữ liệu.",
                errors);
        }

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var managedClasses = await ManagedClassesAsync(ownerUserId, cancellationToken);
            var entity = await managedClasses
                .Include(c => c.ClassSchedules)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Attendances)
                .Include(c => c.Lessons)
                    .ThenInclude(l => l.Homeworks)
                .FirstOrDefaultAsync(c => c.ClassId == request.ClassId, cancellationToken);

            if (entity == null)
            {
                return Failure<ClassMutationResponse>("Không tìm thấy lớp học hoặc bạn không có quyền quản lý.", "ClassId");
            }

            if (entity.Status == "Closed")
            {
                return Failure<ClassMutationResponse>("Lớp đã đóng không thể cập nhật.", "Status");
            }

            if (!TryGetRowVersion(request.RowVersion, out var rowVersion))
            {
                return Failure<ClassMutationResponse>("Phiên bản dữ liệu không hợp lệ. Vui lòng tải lại trang.", "RowVersion");
            }

            _context.Entry(entity).Property(c => c.RowVersion).OriginalValue = rowVersion;

            var today = GetVietnamToday();
            var protectedLessons = entity.Lessons
                .Where(l => l.LessonDate <= today || l.Attendances.Count > 0 || l.Homeworks.Count > 0)
                .OrderBy(l => l.LessonDate)
                .ThenBy(l => l.StartTime)
                .ToList();
            var removableLessons = entity.Lessons.Except(protectedLessons).ToList();

            if (request.TotalSessions < protectedLessons.Count)
            {
                return Failure<ClassMutationResponse>(
                    $"Tổng số buổi không được nhỏ hơn {protectedLessons.Count} buổi đã phát sinh.",
                    "TotalSessions");
            }

            var remaining = request.TotalSessions - protectedLessons.Count;
            var planningStart = request.StartDate!.Value > today
                ? request.StartDate.Value
                : today.AddDays(1);
            var futurePlans = remaining == 0
                ? Array.Empty<PlannedClassLesson>()
                : _lessonPlanner.Build(planningStart, remaining, request.Schedules).ToArray();

            var conflicts = await FindConflictsAsync(entity.ClassId, request, futurePlans, cancellationToken);

            if (conflicts.Count > 0)
            {
                return ClassOperationResult<ClassMutationResponse>.Failure(
                    "Giáo viên hoặc phòng học đang bị trùng lịch.",
                    BuildConflictErrors(conflicts));
            }

            _context.Lessons.RemoveRange(removableLessons);

            var usedScheduleIds = protectedLessons
                .Where(l => l.ClassScheduleId.HasValue)
                .Select(l => l.ClassScheduleId!.Value)
                .ToHashSet();
            _context.ClassSchedules.RemoveRange(
                entity.ClassSchedules.Where(s => !usedScheduleIds.Contains(s.ClassScheduleId)));

            var schedulesByKey = entity.ClassSchedules
                .Where(s => usedScheduleIds.Contains(s.ClassScheduleId))
                .ToDictionary(GetScheduleKey);

            foreach (var scheduleRequest in request.Schedules)
            {
                var key = GetScheduleKey(scheduleRequest);

                if (!schedulesByKey.ContainsKey(key))
                {
                    var schedule = new ClassSchedule
                    {
                        ClassId = entity.ClassId,
                        DayOfWeek = scheduleRequest.DayOfWeek,
                        StartTime = scheduleRequest.StartTime!.Value,
                        EndTime = scheduleRequest.EndTime!.Value
                    };
                    entity.ClassSchedules.Add(schedule);
                    schedulesByKey[key] = schedule;
                }
            }

            for (var index = 0; index < futurePlans.Length; index++)
            {
                var plan = futurePlans[index];
                entity.Lessons.Add(new Lesson
                {
                    LessonTitle = $"Buổi {protectedLessons.Count + index + 1:00}",
                    LessonDate = plan.LessonDate,
                    SessionNumber = protectedLessons.Count + index + 1,
                    StartTime = plan.StartTime,
                    EndTime = plan.EndTime,
                    Status = "Scheduled",
                    ClassSchedule = schedulesByKey[GetScheduleKey(plan)]
                });
            }

            var roomName = await _context.Rooms
                .Where(r => r.RoomId == request.RoomId && r.CenterId == entity.CenterId)
                .Select(r => r.RoomName)
                .SingleAsync(cancellationToken);

            entity.ClassName = request.ClassName;
            entity.CourseId = request.CourseId;
            entity.TeacherId = request.TeacherId;
            entity.RoomId = request.RoomId;
            entity.Room = roomName;
            entity.StartDate = request.StartDate.Value;
            entity.TotalSessions = request.TotalSessions;
            entity.ScheduleText = BuildScheduleText(request.Schedules);
            entity.EndDate = futurePlans.LastOrDefault()?.LessonDate
                ?? protectedLessons.LastOrDefault()?.LessonDate
                ?? request.StartDate.Value;
            entity.UpdatedAt = GetVietnamNow();
            entity.UpdatedByUserId = ownerUserId;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ClassOperationResult<ClassMutationResponse>.Success(
                new ClassMutationResponse(entity.ClassId, entity.Status),
                "Cập nhật lớp học thành công.");
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(exception, "Concurrency conflict updating class {ClassId}.", request.ClassId);
            return Failure<ClassMutationResponse>(
                "Lớp học vừa được người khác cập nhật. Vui lòng tải lại trang và thử lại.",
                "RowVersion");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Could not update class {ClassId}.", request.ClassId);
            return Failure<ClassMutationResponse>(
                "Không thể cập nhật lớp học do dữ liệu đã thay đổi. Vui lòng thử lại.",
                string.Empty);
        }
    }

    public async Task<ClassOperationResult<ClassMutationResponse>> CloseAsync(
        int ownerUserId,
        int classId,
        CancellationToken cancellationToken = default)
    {
        var managedClasses = await ManagedClassesAsync(ownerUserId, cancellationToken);
        var entity = await managedClasses
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.ClassId == classId, cancellationToken);

        if (entity == null)
        {
            return Failure<ClassMutationResponse>("Không tìm thấy lớp học hoặc bạn không có quyền quản lý.", "ClassId");
        }

        if (entity.Status == "Closed")
        {
            return Failure<ClassMutationResponse>("Lớp học đã được đóng trước đó.", "Status");
        }

        var now = GetVietnamNow();
        foreach (var lesson in entity.Lessons.Where(l => l.Status == "Scheduled"))
        {
            lesson.Status = "Cancelled";
        }

        entity.Status = "Closed";
        entity.ClosedAt = now;
        entity.ClosedByUserId = ownerUserId;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = ownerUserId;
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<ClassMutationResponse>.Success(
            new ClassMutationResponse(entity.ClassId, entity.Status),
            "Đóng lớp học thành công.");
    }

    public async Task<ClassOperationResult<ClassMutationResponse>> SoftDeleteAsync(
        int ownerUserId,
        int classId,
        CancellationToken cancellationToken = default)
    {
        var managedClasses = await ManagedClassesAsync(ownerUserId, cancellationToken);
        var entity = await managedClasses
            .Include(c => c.Enrollments)
            .Include(c => c.Invoices)
            .Include(c => c.Grades)
            .Include(c => c.Lessons)
                .ThenInclude(l => l.Attendances)
            .Include(c => c.Lessons)
                .ThenInclude(l => l.Homeworks)
            .FirstOrDefaultAsync(c => c.ClassId == classId, cancellationToken);

        if (entity == null)
        {
            return Failure<ClassMutationResponse>("Không tìm thấy lớp học hoặc bạn không có quyền quản lý.", "ClassId");
        }

        var hasBusinessData =
            entity.Enrollments.Count > 0 ||
            entity.Invoices.Count > 0 ||
            entity.Grades.Count > 0 ||
            entity.Lessons.Any(l =>
                l.LessonDate <= GetVietnamToday() ||
                l.Attendances.Count > 0 ||
                l.Homeworks.Count > 0 ||
                l.Status is "Completed" or "Rescheduled");

        if (hasBusinessData)
        {
            return Failure<ClassMutationResponse>(
                "Lớp đã phát sinh dữ liệu nghiệp vụ nên không thể xóa. Hãy đóng lớp để giữ lịch sử.",
                "ClassId");
        }

        var now = GetVietnamNow();
        foreach (var lesson in entity.Lessons.Where(l => l.Status == "Scheduled"))
        {
            lesson.Status = "Cancelled";
        }

        entity.Status = "Closed";
        entity.ClosedAt ??= now;
        entity.ClosedByUserId ??= ownerUserId;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.DeletedByUserId = ownerUserId;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = ownerUserId;
        await _context.SaveChangesAsync(cancellationToken);

        return ClassOperationResult<ClassMutationResponse>.Success(
            new ClassMutationResponse(entity.ClassId, entity.Status),
            "Xóa lớp học thành công.");
    }

    public async Task<ClassOperationResult<ClassPagedResponse>> GetClassesAsync(
        int ownerUserId,
        ClassQuery query,
        CancellationToken cancellationToken = default)
    {
        var managedClasses = await ManagedClassesAsync(ownerUserId, cancellationToken);
        var queryable = managedClasses
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.Teacher)
                .ThenInclude(t => t.User)
            .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
            .Include(c => c.RoomNavigation)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var search = query.Keyword.Trim();
            queryable = queryable.Where(c =>
                c.ClassName.Contains(search) ||
                c.ClassCode.Contains(search) ||
                c.Course.CourseName.Contains(search) ||
                c.Teacher.User.FullName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            queryable = queryable.Where(c => c.Status == query.Status);
        }

        if (query.CourseId.HasValue)
        {
            queryable = queryable.Where(c => c.CourseId == query.CourseId.Value);
        }

        if (query.TeacherId.HasValue)
        {
            queryable = queryable.Where(c => c.TeacherId == query.TeacherId.Value);
        }

        var totalItems = await queryable.CountAsync(cancellationToken);

        var classes = await queryable
            .OrderByDescending(c => c.StartDate)
            .ThenByDescending(c => c.ClassId)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = classes.Select(c => new ClassListItemDto(
            c.ClassId,
            c.ClassCode,
            c.ClassName,
            c.Course.CourseName,
            c.Teacher.User.FullName,
            c.StartDate,
            c.EndDate,
            c.Enrollments.Count(e => e.Status == "Đang học" && !e.Student.IsDeleted),
            c.ScheduleText ?? string.Empty,
            string.IsNullOrWhiteSpace(c.ScheduleText) ? new[] { "-" } : c.ScheduleText.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            c.RoomNavigation != null ? c.RoomNavigation.RoomName : string.Empty,
            c.RoomNavigation != null ? c.RoomNavigation.RoomName : "-",
            $"{c.StartDate:dd/MM/yyyy} - {c.EndDate:dd/MM/yyyy}",
            c.Status,
            GetDisplaySchedule(c.RoomNavigation != null ? c.RoomNavigation.RoomName : c.Room, c.ScheduleText),
            GetStatusText(c.Status),
            GetStatusBadgeClass(c.Status)
        )).ToList();

        var response = new ClassPagedResponse(
            items,
            totalItems,
            query.PageNumber,
            query.PageSize,
            (int)Math.Ceiling(totalItems / (double)query.PageSize)
        );

        return ClassOperationResult<ClassPagedResponse>.Success(response, "Success");
    }

    public async Task<ClassOperationResult<ClassDropdownOptionsResponse>> GetClassOptionsAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var centerId = await _context.Centers
            .Where(c => c.Status == "Active" && (c.OwnerUserId == ownerUserId || _context.CenterUsers.Any(cu => cu.CenterId == c.CenterId && cu.UserId == ownerUserId && cu.UserType == "OWNER" && cu.Status == "Active")))
            .Select(c => c.CenterId)
            .FirstOrDefaultAsync(cancellationToken);

        if (centerId == 0)
        {
            return ClassOperationResult<ClassDropdownOptionsResponse>.Failure("Không tìm thấy trung tâm.", new Dictionary<string, string[]>());
        }

        var courses = await _context.Courses
            .Where(c => c.CenterId == centerId && c.Status == "Active" && !c.IsDeleted)
            .OrderBy(c => c.CourseName)
            .Select(c => new CourseOptionDto(c.CourseId, c.CourseName, c.TotalSessions))
            .ToListAsync(cancellationToken);

        var teachers = await _context.Teachers
            .Where(t => t.CenterId == centerId && t.Status == "Active" && !t.IsDeleted && !t.User.IsDeleted)
            .OrderBy(t => t.User.FullName)
            .Select(t => new TeacherOptionDto(t.TeacherId, t.User.FullName))
            .ToListAsync(cancellationToken);

        var rooms = await _context.Rooms
            .Where(r => r.CenterId == centerId && r.Status == "Active" && !r.IsDeleted)
            .OrderBy(r => r.RoomName)
            .Select(r => new RoomOptionDto(r.RoomId, r.RoomName, r.RoomCode, r.RoomName))
            .ToListAsync(cancellationToken);

        return ClassOperationResult<ClassDropdownOptionsResponse>.Success(
            new ClassDropdownOptionsResponse(courses, teachers, rooms), "Success");
    }

    private static string GetDisplaySchedule(string? room, string? scheduleText)
    {
        if (string.IsNullOrWhiteSpace(room))
            return scheduleText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(scheduleText))
            return room;
        return $"{scheduleText} - {room}";
    }

    private static string GetStatusText(string status) => status.ToUpperInvariant() switch
    {
        "ACTIVE" => "Đang hoạt động",
        "INACTIVE" => "Tạm dừng",
        "CLOSED" => "Đã đóng",
        _ => "Không xác định"
    };

    private static string GetStatusBadgeClass(string status) => status.ToUpperInvariant() switch
    {
        "ACTIVE" => "bg-green-100 text-green-700",
        "INACTIVE" => "bg-yellow-100 text-yellow-700",
        "CLOSED" => "bg-gray-200 text-gray-700",
        _ => "bg-red-100 text-red-700"
    };

    private async Task<IReadOnlyDictionary<string, string[]>> ValidateAsync(
        int ownerUserId,
        UpdateClassRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var managedClasses = await ManagedClassesAsync(ownerUserId, cancellationToken);
        var entity = await managedClasses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClassId == request.ClassId, cancellationToken);

        if (entity == null || entity.CenterId != request.CenterId)
        {
            AddError(errors, "ClassId", "Lớp học không hợp lệ hoặc không thuộc trung tâm.");
            return ToErrors(errors);
        }

        if (string.IsNullOrWhiteSpace(request.ClassName))
            AddError(errors, "ClassName", "Vui lòng nhập tên lớp.");
        else if (request.ClassName.Length > 150)
            AddError(errors, "ClassName", "Tên lớp tối đa 150 ký tự.");

        if (!request.StartDate.HasValue)
            AddError(errors, "StartDate", "Vui lòng chọn ngày khai giảng.");
        if (request.TotalSessions is < 1 or > 1000)
            AddError(errors, "TotalSessions", "Tổng số buổi phải từ 1 đến 1000.");

        ValidateSchedules(request.Schedules, errors);

        if (request.StartDate.HasValue)
        {
            var firstProtectedLessonDate = await _context.Lessons
                .AsNoTracking()
                .Where(l =>
                    l.ClassId == request.ClassId &&
                    (l.LessonDate <= GetVietnamToday() ||
                     l.Attendances.Any() ||
                     l.Homeworks.Any()))
                .OrderBy(l => l.LessonDate)
                .Select(l => (DateOnly?)l.LessonDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (firstProtectedLessonDate.HasValue && request.StartDate.Value > firstProtectedLessonDate.Value)
            {
                AddError(errors, "StartDate", "Ngày khai giảng không được sau buổi học đã phát sinh đầu tiên.");
            }
        }

        if (!await _context.Courses.AsNoTracking().AnyAsync(c =>
                c.CourseId == request.CourseId && c.CenterId == request.CenterId &&
                !c.IsDeleted && (c.Status == "Active" || c.CourseId == entity.CourseId), cancellationToken))
            AddError(errors, "CourseId", "Môn học không hợp lệ hoặc không còn hoạt động.");

        if (!await _context.Teachers.AsNoTracking().AnyAsync(t =>
                t.TeacherId == request.TeacherId && t.CenterId == request.CenterId &&
                !t.IsDeleted && !t.User.IsDeleted &&
                (t.Status == "Active" || t.TeacherId == entity.TeacherId), cancellationToken))
            AddError(errors, "TeacherId", "Giáo viên không hợp lệ hoặc không còn hoạt động.");

        if (!await _context.Rooms.AsNoTracking().AnyAsync(r =>
                r.RoomId == request.RoomId && r.CenterId == request.CenterId &&
                !r.IsDeleted && (r.Status == "Active" || r.RoomId == entity.RoomId), cancellationToken))
            AddError(errors, "RoomId", "Phòng học không hợp lệ hoặc không còn hoạt động.");

        return ToErrors(errors);
    }

    private async Task<List<ClassConflictItem>> FindConflictsAsync(
        int excludedClassId,
        UpdateClassRequest request,
        IReadOnlyList<PlannedClassLesson> plans,
        CancellationToken cancellationToken)
    {
        if (plans.Count == 0) return [];
        var dates = plans.Select(p => p.LessonDate).Distinct().ToList();
        var existing = await _context.Lessons.AsNoTracking()
            .Where(l =>
                l.ClassId != excludedClassId &&
                dates.Contains(l.LessonDate) &&
                l.Status != "Cancelled" &&
                !l.Class.IsDeleted &&
                l.Class.Status != "Closed" &&
                l.Class.CenterId == request.CenterId &&
                (l.Class.TeacherId == request.TeacherId || l.Class.RoomId == request.RoomId) &&
                l.StartTime != null && l.EndTime != null)
            .Select(l => new
            {
                l.LessonDate, StartTime = l.StartTime!.Value, EndTime = l.EndTime!.Value,
                l.Class.TeacherId, l.Class.RoomId, l.Class.ClassCode, l.Class.ClassName,
                TeacherName = l.Class.Teacher.User.FullName,
                RoomName = l.Class.RoomNavigation.RoomName
            }).ToListAsync(cancellationToken);

        var result = new List<ClassConflictItem>();
        foreach (var plan in plans)
        foreach (var item in existing.Where(x =>
                     x.LessonDate == plan.LessonDate && x.StartTime < plan.EndTime && x.EndTime > plan.StartTime))
        {
            if (item.TeacherId == request.TeacherId)
                result.Add(new("Teacher", item.TeacherName, plan.LessonDate, plan.StartTime, plan.EndTime, item.ClassCode, item.ClassName));
            if (item.RoomId == request.RoomId)
                result.Add(new("Room", item.RoomName, plan.LessonDate, plan.StartTime, plan.EndTime, item.ClassCode, item.ClassName));
        }
        return result.Distinct().ToList();
    }

    private async Task<IQueryable<Class>> ManagedClassesAsync(int ownerUserId, CancellationToken cancellationToken)
    {
        var centerId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
        return _context.Classes.Where(c => !c.IsDeleted && c.CenterId == centerId);
    }

    private async Task<ClassCreateOptionsResponse> IncludeCurrentOptionsAsync(
        Class entity,
        ClassCreateOptionsResponse options,
        CancellationToken cancellationToken)
    {
        var courses = options.Courses.ToList();
        if (courses.All(c => c.CourseId != entity.CourseId))
        {
            var current = await _context.Courses.AsNoTracking()
                .Where(c => c.CourseId == entity.CourseId && !c.IsDeleted)
                .Select(c => new ClassCourseOption(c.CourseId, c.CourseCode, c.CourseName, c.TotalSessions))
                .FirstOrDefaultAsync(cancellationToken);
            if (current != null) courses.Add(current);
        }

        var teachers = options.Teachers.ToList();
        if (teachers.All(t => t.TeacherId != entity.TeacherId))
        {
            var current = await _context.Teachers.AsNoTracking()
                .Where(t => t.TeacherId == entity.TeacherId && !t.IsDeleted && !t.User.IsDeleted)
                .Select(t => new ClassTeacherOption(t.TeacherId, t.TeacherCode, t.User.FullName))
                .FirstOrDefaultAsync(cancellationToken);
            if (current != null) teachers.Add(current);
        }

        var rooms = options.Rooms.ToList();
        if (rooms.All(r => r.RoomId != entity.RoomId))
        {
            var current = await _context.Rooms.AsNoTracking()
                .Where(r => r.RoomId == entity.RoomId && !r.IsDeleted)
                .Select(r => new ClassRoomOption(r.RoomId, r.RoomCode, r.RoomName))
                .FirstOrDefaultAsync(cancellationToken);
            if (current != null) rooms.Add(current);
        }

        return new ClassCreateOptionsResponse(
            options.SuggestedClassCode,
            courses.OrderBy(c => c.CourseName).ToList(),
            teachers.OrderBy(t => t.TeacherName).ToList(),
            rooms.OrderBy(r => r.RoomName).ToList(),
            options.StudyShifts);
    }

    private static void ValidateSchedules(IReadOnlyList<ClassScheduleRequest> schedules, IDictionary<string, List<string>> errors)
    {
        if (schedules.Count == 0) { AddError(errors, "Schedules", "Vui lòng thêm ít nhất một lịch học."); return; }
        for (var i = 0; i < schedules.Count; i++)
        {
            var s = schedules[i];
            if (s.DayOfWeek is < 1 or > 7) AddError(errors, $"Schedules[{i}].DayOfWeek", "Thứ học không hợp lệ.");
            if (!s.StartTime.HasValue) AddError(errors, $"Schedules[{i}].StartTime", "Vui lòng chọn giờ bắt đầu.");
            if (!s.EndTime.HasValue) AddError(errors, $"Schedules[{i}].EndTime", "Vui lòng chọn giờ kết thúc.");
            else if (s.StartTime.HasValue && s.EndTime <= s.StartTime) AddError(errors, $"Schedules[{i}].EndTime", "Giờ kết thúc phải sau giờ bắt đầu.");
        }
        var valid = schedules.Where(s => s.StartTime.HasValue && s.EndTime.HasValue).ToList();
        if (valid.GroupBy(GetScheduleKey).Any(g => g.Count() > 1))
            AddError(errors, "Schedules", "Lịch học bị trùng hoàn toàn.");
        if (valid.GroupBy(s => s.DayOfWeek).Any(g =>
                g.OrderBy(s => s.StartTime).Zip(g.OrderBy(s => s.StartTime).Skip(1),
                    (a, b) => b.StartTime < a.EndTime).Any(x => x)))
            AddError(errors, "Schedules", "Các lịch học trong cùng một ngày không được chồng giờ.");
    }

    private static IReadOnlyDictionary<string, string[]> BuildConflictErrors(IEnumerable<ClassConflictItem> conflicts) =>
        conflicts.GroupBy(c => c.ResourceType == "Teacher" ? "TeacherId" : "RoomId")
            .ToDictionary(g => g.Key, g => g.Select(c =>
                $"{c.ResourceName} bị trùng với {c.ExistingClassCode} - {c.ExistingClassName} ngày {c.LessonDate:dd/MM/yyyy}, {c.StartTime:HH\\:mm}-{c.EndTime:HH\\:mm}.").Distinct().ToArray());

    private static string BuildScheduleText(IEnumerable<ClassScheduleRequest> schedules) =>
        string.Join("; ", schedules.OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => $"{GetDayLabel(s.DayOfWeek)} - {s.StartTime:HH\\:mm} - {s.EndTime:HH\\:mm}"));

    private static string GetDayLabel(byte day) => day switch
    {
        1 => "Thứ 2", 2 => "Thứ 3", 3 => "Thứ 4", 4 => "Thứ 5",
        5 => "Thứ 6", 6 => "Thứ 7", 7 => "Chủ nhật", _ => string.Empty
    };
    private static string GetScheduleKey(ClassScheduleRequest s) => $"{s.DayOfWeek}:{s.StartTime:HH\\:mm}:{s.EndTime:HH\\:mm}";
    private static string GetScheduleKey(ClassSchedule s) => $"{s.DayOfWeek}:{s.StartTime:HH\\:mm}:{s.EndTime:HH\\:mm}";
    private static string GetScheduleKey(PlannedClassLesson s) => $"{s.DayOfWeek}:{s.StartTime:HH\\:mm}:{s.EndTime:HH\\:mm}";
    private static void Normalize(UpdateClassRequest request) { request.ClassName = (request.ClassName ?? "").Trim(); request.Schedules ??= []; }
    private static bool TryGetRowVersion(string value, out byte[] bytes) { try { bytes = Convert.FromBase64String(value); return bytes.Length > 0; } catch { bytes = []; return false; } }
    private static void AddError(IDictionary<string, List<string>> errors, string key, string message) { if (!errors.TryGetValue(key, out var list)) errors[key] = list = []; if (!list.Contains(message)) list.Add(message); }
    private static IReadOnlyDictionary<string, string[]> ToErrors(IDictionary<string, List<string>> errors) => errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
    private static ClassOperationResult<T> Failure<T>(string message, string key) => ClassOperationResult<T>.Failure(message, new Dictionary<string, string[]> { [key] = [message] });
    private static DateOnly GetVietnamToday() => DateOnly.FromDateTime(GetVietnamNow());
    private static DateTime GetVietnamNow() { try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")); } catch { return DateTime.UtcNow.AddHours(7); } }
}
