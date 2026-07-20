using System;
using System.Collections.Generic;

namespace EduBridge.Models.DTOs.TeacherDashboard
{
    public class DashboardResponseDto
    {
        public string TeacherName { get; set; } = string.Empty;
        public int TotalClasses { get; set; }
        public int TotalStudents { get; set; }
        public int UngradedAssignmentsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
        public List<ScheduleDto> TodaySchedules { get; set; } = new List<ScheduleDto>();
        public List<AssignmentDto> RecentAssignments { get; set; } = new List<AssignmentDto>();
        public List<MessageDto> RecentMessages { get; set; } = new List<MessageDto>();
    }

    public class ScheduleDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string TimeRange { get; set; } = string.Empty;
        public string Room { get; set; } = string.Empty;
    }

    public class AssignmentDto
    {
        public int HomeworkId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public int SubmittedCount { get; set; }
        public int TotalStudents { get; set; }
    }

    public class MessageDto
    {
        public int MessageId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = string.Empty;
        public string ShortContent { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
    }
}
