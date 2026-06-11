namespace EduBridge.Contracts.Dashboard;

public sealed record TeacherDashboardSummaryResponse(
    string TeacherName,
    int TotalClasses,
    int TotalStudents,
    int UngradedAssignments,
    int UnreadMessages,
    IReadOnlyList<TeacherDashboardScheduleDto> TodaySchedules,
    IReadOnlyList<TeacherDashboardAssignmentDto> RecentAssignments,
    IReadOnlyList<TeacherDashboardMessageDto> RecentMessages
);

public sealed record TeacherDashboardScheduleDto(
    string ClassName,
    string Topic,
    string TimeRange,
    string Room
);

public sealed record TeacherDashboardAssignmentDto(
    string Title,
    string ClassName,
    DateTime CreatedAt,
    int Submitted,
    int Total,
    int PercentComplete
);

public sealed record TeacherDashboardMessageDto(
    string SenderName,
    string ParentInfo,
    string Content,
    string TimeAgo,
    string Avatar
);
