using System.Data;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Models;
using Microsoft.EntityFrameworkCore;
using EduBridge.Services.Auth;

namespace EduBridge.Services.Classes;

public sealed class ClassCreationService : IClassCreationService
{
    private readonly AppDbContext _context;
    private readonly IClassLessonPlanner _lessonPlanner;
    private readonly ILogger<ClassCreationService> _logger;
    private readonly ICurrentCenterService _currentCenterService;

    public ClassCreationService(
        AppDbContext context,
        IClassLessonPlanner lessonPlanner,
        ILogger<ClassCreationService> logger,
        ICurrentCenterService currentCenterService)
    {
        _context = context;
        _lessonPlanner = lessonPlanner;
        _logger = logger;
        _currentCenterService = currentCenterService;
    }

    public async Task<ClassOperationResult<ClassCreateOptionsResponse>> GetCreateOptionsAsync(
        int ownerUserId,
        int centerId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageCenterAsync(ownerUserId, centerId, cancellationToken))
        {
            return Failure<ClassCreateOptionsResponse>("Bạn không có quyền quản lý trung tâm này.", "CenterId");
        }

        var courses = await _context.Courses
            .AsNoTracking()
            .Where(c => c.CenterId == centerId && c.Status == "Active" && !c.IsDeleted)
            .OrderBy(c => c.CourseName)
            .Select(c => new ClassCourseOption(c.CourseId, c.CourseCode, c.CourseName, c.TotalSessions))
            .ToListAsync(cancellationToken);

        var teachers = await _context.Teachers
            .AsNoTracking()
            .Where(t =>
                t.CenterId == centerId &&
                t.Status == "Active" &&
                !t.IsDeleted &&
                !t.User.IsDeleted)
            .OrderBy(t => t.User.FullName)
            .Select(t => new ClassTeacherOption(t.TeacherId, t.TeacherCode, t.User.FullName))
            .ToListAsync(cancellationToken);

        var rooms = await _context.Rooms
            .AsNoTracking()
            .Where(r => r.CenterId == centerId && r.Status == "Active" && !r.IsDeleted)
            .OrderBy(r => r.RoomName)
            .ThenBy(r => r.RoomCode)
            .Select(r => new ClassRoomOption(r.RoomId, r.RoomCode, r.RoomName))
            .ToListAsync(cancellationToken);

        var shifts = await _context.StudyShifts
            .AsNoTracking()
            .Where(s => s.CenterId == centerId && s.Status == "Active" && !s.IsDeleted)
            .OrderBy(s => s.StartTime)
            .ThenBy(s => s.ShiftName)
            .Select(s => new ClassStudyShiftOption(
                s.StudyShiftId,
                s.ShiftCode,
                s.ShiftName,
                s.StartTime,
                s.EndTime))
            .ToListAsync(cancellationToken);

        var prefix = $"CLS-{centerId}-{GetVietnamNow():yyyyMM}-";
        var lastNumber = await _context.Classes
            .AsNoTracking()
            .Where(c => c.CenterId == centerId && c.ClassCode.StartsWith(prefix))
            .Select(c => c.ClassCode)
            .ToListAsync(cancellationToken);
        var nextNumber = lastNumber.Select(GetClassCodeSequence).DefaultIfEmpty(0).Max() + 1;

        return ClassOperationResult<ClassCreateOptionsResponse>.Success(
            new ClassCreateOptionsResponse(
                $"{prefix}{nextNumber:0000}",
                courses,
                teachers,
                rooms,
                shifts),
            "Tải dữ liệu tạo lớp thành công.");
    }

    public async Task<ClassOperationResult<ClassCreationResponse>> CreateAsync(
        int ownerUserId,
        CreateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        Normalize(request);
        var validation = await ValidateAsync(ownerUserId, request, cancellationToken);

        if (validation.Errors.Count > 0 || validation.Course == null)
        {
            return ClassOperationResult<ClassCreationResponse>.Failure(
                "Không thể tạo lớp học. Vui lòng kiểm tra lại dữ liệu.",
                validation.Errors);
        }

        var totalSessions = request.TotalSessions ?? validation.Course.TotalSessions;
        var lessonPlans = _lessonPlanner.Build(request.StartDate!.Value, totalSessions, request.Schedules);
        var conflicts = await FindConflictsAsync(request, lessonPlans, cancellationToken);

        if (conflicts.Count > 0)
        {
            return ClassOperationResult<ClassCreationResponse>.Failure(
                "Giáo viên hoặc phòng học đang bị trùng lịch.",
                BuildConflictErrors(conflicts));
        }

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            // Recheck inside the serializable transaction to prevent concurrent bookings.
            conflicts = await FindConflictsAsync(request, lessonPlans, cancellationToken);

            if (conflicts.Count > 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ClassOperationResult<ClassCreationResponse>.Failure(
                    "Giáo viên hoặc phòng học đang bị trùng lịch.",
                    BuildConflictErrors(conflicts));
            }

            var classCode = await GenerateClassCodeAsync(request.CenterId, cancellationToken);
            var roomName = await _context.Rooms
                .Where(r => r.RoomId == request.RoomId && r.CenterId == request.CenterId)
                .Select(r => r.RoomName)
                .SingleAsync(cancellationToken);

            var classEntity = new Class
            {
                CenterId = request.CenterId,
                CourseId = request.CourseId,
                TeacherId = request.TeacherId,
                RoomId = request.RoomId,
                Room = roomName,
                ClassCode = classCode,
                ClassName = request.ClassName,
                ScheduleText = BuildScheduleText(request.Schedules),
                StartDate = request.StartDate.Value,
                EndDate = lessonPlans[^1].LessonDate,
                TotalSessions = totalSessions,
                Status = "Active"
            };

            var scheduleEntities = request.Schedules.ToDictionary(
                GetScheduleKey,
                schedule => new ClassSchedule
                {
                    DayOfWeek = schedule.DayOfWeek,
                    StartTime = schedule.StartTime!.Value,
                    EndTime = schedule.EndTime!.Value
                });

            foreach (var schedule in scheduleEntities.Values)
            {
                classEntity.ClassSchedules.Add(schedule);
            }

            for (var index = 0; index < lessonPlans.Count; index++)
            {
                var lessonPlan = lessonPlans[index];
                classEntity.Lessons.Add(new Lesson
                {
                    LessonTitle = $"Buổi {index + 1:00}",
                    LessonDate = lessonPlan.LessonDate,
                    SessionNumber = index + 1,
                    StartTime = lessonPlan.StartTime,
                    EndTime = lessonPlan.EndTime,
                    Status = "Scheduled",
                    ClassSchedule = scheduleEntities[GetScheduleKey(lessonPlan)]
                });
            }

            _context.Classes.Add(classEntity);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return ClassOperationResult<ClassCreationResponse>.Success(
                new ClassCreationResponse(
                    classEntity.ClassId,
                    classEntity.ClassCode,
                    classEntity.ClassName,
                    classEntity.StartDate,
                    classEntity.EndDate,
                    classEntity.TotalSessions),
                "Thêm lớp học thành công.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Could not create class {ClassName}.", request.ClassName);
            return Failure<ClassCreationResponse>(
                "Không thể tạo lớp học do dữ liệu đã thay đổi. Vui lòng thử lại.",
                string.Empty);
        }
    }

    public async Task<ClassOperationResult<ClassConflictResponse>> CheckConflictsAsync(
        int ownerUserId,
        CreateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        Normalize(request);
        var validation = await ValidateAsync(ownerUserId, request, cancellationToken);

        if (validation.Errors.Count > 0 || validation.Course == null)
        {
            return ClassOperationResult<ClassConflictResponse>.Failure(
                "Không thể kiểm tra lịch. Vui lòng kiểm tra lại dữ liệu.",
                validation.Errors);
        }

        var plans = _lessonPlanner.Build(
            request.StartDate!.Value,
            request.TotalSessions ?? validation.Course.TotalSessions,
            request.Schedules);
        var conflicts = await FindConflictsAsync(request, plans, cancellationToken);

        return ClassOperationResult<ClassConflictResponse>.Success(
            new ClassConflictResponse(conflicts.Count > 0, conflicts),
            conflicts.Count == 0 ? "Không phát hiện lịch trùng." : "Phát hiện lịch bị trùng.");
    }

    private async Task<ValidationState> ValidateAsync(
        int ownerUserId,
        CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (!await CanManageCenterAsync(ownerUserId, request.CenterId, cancellationToken))
        {
            AddError(errors, "CenterId", "Bạn không có quyền quản lý trung tâm này.");
        }

        if (string.IsNullOrWhiteSpace(request.ClassName))
        {
            AddError(errors, "ClassName", "Vui lòng nhập tên lớp.");
        }
        else if (request.ClassName.Length > 150)
        {
            AddError(errors, "ClassName", "Tên lớp tối đa 150 ký tự.");
        }

        if (!request.StartDate.HasValue)
        {
            AddError(errors, "StartDate", "Vui lòng chọn ngày khai giảng.");
        }
        else if (request.StartDate.Value < GetVietnamToday())
        {
            AddError(errors, "StartDate", "Ngày khai giảng không được nằm trong quá khứ.");
        }

        if (request.TotalSessions.HasValue && request.TotalSessions is < 1 or > 1000)
        {
            AddError(errors, "TotalSessions", "Tổng số buổi phải từ 1 đến 1000.");
        }

        ValidateSchedules(request.Schedules, errors);

        var course = await _context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.CourseId == request.CourseId &&
                c.CenterId == request.CenterId &&
                c.Status == "Active" &&
                !c.IsDeleted,
                cancellationToken);

        if (course == null)
        {
            AddError(errors, "CourseId", "Môn học không hợp lệ hoặc không thuộc trung tâm.");
        }
        else if (!request.TotalSessions.HasValue && course.TotalSessions < 1)
        {
            AddError(errors, "TotalSessions", "Môn học chưa có tổng số buổi hợp lệ. Vui lòng nhập tổng số buổi cho lớp.");
        }

        var teacherExists = await _context.Teachers
            .AsNoTracking()
            .AnyAsync(t =>
                t.TeacherId == request.TeacherId &&
                t.CenterId == request.CenterId &&
                t.Status == "Active" &&
                !t.IsDeleted &&
                !t.User.IsDeleted,
                cancellationToken);

        if (!teacherExists)
        {
            AddError(errors, "TeacherId", "Giáo viên không hợp lệ hoặc không thuộc trung tâm.");
        }

        var roomExists = await _context.Rooms
            .AsNoTracking()
            .AnyAsync(r =>
                r.RoomId == request.RoomId &&
                r.CenterId == request.CenterId &&
                r.Status == "Active" &&
                !r.IsDeleted,
                cancellationToken);

        if (!roomExists)
        {
            AddError(errors, "RoomId", "Phòng học không hợp lệ hoặc không thuộc trung tâm.");
        }

        return new ValidationState(
            course,
            errors.ToDictionary(item => item.Key, item => item.Value.ToArray()));
    }

    private static void ValidateSchedules(
        IReadOnlyList<ClassScheduleRequest> schedules,
        IDictionary<string, List<string>> errors)
    {
        if (schedules.Count == 0)
        {
            AddError(errors, "Schedules", "Vui lòng thêm ít nhất một lịch học.");
            return;
        }

        var exactKeys = new HashSet<string>();

        for (var index = 0; index < schedules.Count; index++)
        {
            var schedule = schedules[index];
            var prefix = $"Schedules[{index}]";

            if (schedule.DayOfWeek is < 1 or > 7)
            {
                AddError(errors, $"{prefix}.DayOfWeek", "Thứ học không hợp lệ.");
            }

            if (!schedule.StartTime.HasValue)
            {
                AddError(errors, $"{prefix}.StartTime", "Vui lòng chọn giờ bắt đầu.");
            }

            if (!schedule.EndTime.HasValue)
            {
                AddError(errors, $"{prefix}.EndTime", "Vui lòng chọn giờ kết thúc.");
            }
            else if (schedule.StartTime.HasValue && schedule.EndTime <= schedule.StartTime)
            {
                AddError(errors, $"{prefix}.EndTime", "Giờ kết thúc phải sau giờ bắt đầu.");
            }

            if (schedule.StartTime.HasValue && schedule.EndTime.HasValue)
            {
                var key = GetScheduleKey(schedule);

                if (!exactKeys.Add(key))
                {
                    AddError(errors, prefix, "Lịch học bị trùng hoàn toàn.");
                }
            }
        }

        foreach (var dayGroup in schedules
                     .Where(s => s.StartTime.HasValue && s.EndTime.HasValue)
                     .GroupBy(s => s.DayOfWeek))
        {
            var ordered = dayGroup.OrderBy(s => s.StartTime).ToList();

            for (var index = 1; index < ordered.Count; index++)
            {
                if (ordered[index].StartTime < ordered[index - 1].EndTime)
                {
                    AddError(errors, "Schedules", "Các lịch học trong cùng một ngày không được chồng giờ.");
                    break;
                }
            }
        }
    }

    private async Task<List<ClassConflictItem>> FindConflictsAsync(
        CreateClassRequest request,
        IReadOnlyList<PlannedClassLesson> plans,
        CancellationToken cancellationToken)
    {
        var dates = plans.Select(plan => plan.LessonDate).Distinct().ToList();
        var existing = await _context.Lessons
            .AsNoTracking()
            .Where(lesson =>
                dates.Contains(lesson.LessonDate) &&
                lesson.Status != "Cancelled" &&
                !lesson.Class.IsDeleted &&
                lesson.Class.CenterId == request.CenterId &&
                lesson.Class.Status != "Closed" &&
                (lesson.Class.TeacherId == request.TeacherId || lesson.Class.RoomId == request.RoomId) &&
                lesson.StartTime != null &&
                lesson.EndTime != null)
            .Select(lesson => new
            {
                lesson.LessonDate,
                StartTime = lesson.StartTime!.Value,
                EndTime = lesson.EndTime!.Value,
                lesson.Class.TeacherId,
                lesson.Class.RoomId,
                lesson.Class.ClassCode,
                lesson.Class.ClassName,
                TeacherName = lesson.Class.Teacher.User.FullName,
                RoomName = lesson.Class.RoomNavigation.RoomName
            })
            .ToListAsync(cancellationToken);

        var result = new List<ClassConflictItem>();

        foreach (var plan in plans)
        {
            foreach (var item in existing.Where(item =>
                         item.LessonDate == plan.LessonDate &&
                         item.StartTime < plan.EndTime &&
                         item.EndTime > plan.StartTime))
            {
                if (item.TeacherId == request.TeacherId)
                {
                    result.Add(new ClassConflictItem(
                        "Teacher",
                        item.TeacherName,
                        plan.LessonDate,
                        plan.StartTime,
                        plan.EndTime,
                        item.ClassCode,
                        item.ClassName));
                }

                if (item.RoomId == request.RoomId)
                {
                    result.Add(new ClassConflictItem(
                        "Room",
                        item.RoomName,
                        plan.LessonDate,
                        plan.StartTime,
                        plan.EndTime,
                        item.ClassCode,
                        item.ClassName));
                }
            }
        }

        return result
            .DistinctBy(item => new
            {
                item.ResourceType,
                item.LessonDate,
                item.StartTime,
                item.EndTime,
                item.ExistingClassCode
            })
            .OrderBy(item => item.LessonDate)
            .ThenBy(item => item.StartTime)
            .ToList();
    }

    private async Task<bool> CanManageCenterAsync(
        int ownerUserId,
        int centerId,
        CancellationToken cancellationToken)
    {
        var currentCenterId = await _currentCenterService.GetCenterIdAsync(cancellationToken);
        return currentCenterId == centerId;
    }

    private async Task<string> GenerateClassCodeAsync(int centerId, CancellationToken cancellationToken)
    {
        var yearMonth = GetVietnamNow().ToString("yyyyMM");
        var counter = await _context.ClassCodeCounters
            .SingleOrDefaultAsync(c => c.CenterId == centerId && c.YearMonth == yearMonth, cancellationToken);

        if (counter == null)
        {
            var prefix = $"CLS-{centerId}-{yearMonth}-";
            var existingCodes = await _context.Classes
                .AsNoTracking()
                .Where(c => c.CenterId == centerId && c.ClassCode.StartsWith(prefix))
                .Select(c => c.ClassCode)
                .ToListAsync(cancellationToken);
            counter = new ClassCodeCounter
            {
                CenterId = centerId,
                YearMonth = yearMonth,
                LastNumber = existingCodes.Select(GetClassCodeSequence).DefaultIfEmpty(0).Max()
            };
            _context.ClassCodeCounters.Add(counter);
        }

        counter.LastNumber += 1;
        return $"CLS-{centerId}-{yearMonth}-{counter.LastNumber:0000}";
    }

    private static void Normalize(CreateClassRequest request)
    {
        request.ClassName = (request.ClassName ?? string.Empty).Trim();
        request.Schedules ??= new List<ClassScheduleRequest>();
    }

    private static DateOnly GetVietnamToday()
        => DateOnly.FromDateTime(GetVietnamNow());

    private static DateTime GetVietnamNow()
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTime.UtcNow.AddHours(7);
        }
    }

    private static string BuildScheduleText(IEnumerable<ClassScheduleRequest> schedules) =>
        string.Join("; ", schedules
            .OrderBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .Select(schedule =>
                $"{GetDayLabel(schedule.DayOfWeek)} - {schedule.StartTime:HH\\:mm} - {schedule.EndTime:HH\\:mm}"));

    private static string GetDayLabel(byte dayOfWeek) => dayOfWeek switch
    {
        1 => "Thứ 2",
        2 => "Thứ 3",
        3 => "Thứ 4",
        4 => "Thứ 5",
        5 => "Thứ 6",
        6 => "Thứ 7",
        7 => "Chủ nhật",
        _ => string.Empty
    };

    private static string GetScheduleKey(ClassScheduleRequest schedule) =>
        $"{schedule.DayOfWeek}:{schedule.StartTime:HH\\:mm}:{schedule.EndTime:HH\\:mm}";

    private static string GetScheduleKey(PlannedClassLesson lesson) =>
        $"{lesson.DayOfWeek}:{lesson.StartTime:HH\\:mm}:{lesson.EndTime:HH\\:mm}";

    private static int GetClassCodeSequence(string? classCode) =>
        int.TryParse(classCode?.Split('-').LastOrDefault(), out var sequence) ? sequence : 0;

    private static IReadOnlyDictionary<string, string[]> BuildConflictErrors(
        IReadOnlyList<ClassConflictItem> conflicts)
    {
        var errors = new Dictionary<string, string[]>();
        var teacherConflicts = conflicts.Where(item => item.ResourceType == "Teacher").ToList();
        var roomConflicts = conflicts.Where(item => item.ResourceType == "Room").ToList();

        if (teacherConflicts.Count > 0)
        {
            errors["TeacherId"] = teacherConflicts
                .Select(FormatConflict)
                .Distinct()
                .ToArray();
        }

        if (roomConflicts.Count > 0)
        {
            errors["RoomId"] = roomConflicts
                .Select(FormatConflict)
                .Distinct()
                .ToArray();
        }

        return errors;
    }

    private static string FormatConflict(ClassConflictItem item) =>
        $"{item.ResourceName} bị trùng với {item.ExistingClassCode} - {item.ExistingClassName} ngày " +
        $"{item.LessonDate:dd/MM/yyyy}, {item.StartTime:HH\\:mm}-{item.EndTime:HH\\:mm}.";

    private static void AddError(
        IDictionary<string, List<string>> errors,
        string key,
        string message)
    {
        if (!errors.TryGetValue(key, out var messages))
        {
            messages = new List<string>();
            errors[key] = messages;
        }

        if (!messages.Contains(message))
        {
            messages.Add(message);
        }
    }

    private static ClassOperationResult<T> Failure<T>(string message, string key) =>
        ClassOperationResult<T>.Failure(
            message,
            new Dictionary<string, string[]> { [key] = new[] { message } });

    private sealed record ValidationState(
        Course? Course,
        IReadOnlyDictionary<string, string[]> Errors);
}
