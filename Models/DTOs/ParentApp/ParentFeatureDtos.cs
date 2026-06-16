using System;
using System.Collections.Generic;

namespace EduBridge.Models.DTOs.ParentApp;

public class ParentScheduleDto
{
    public int LessonId { get; set; }
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public DateOnly LessonDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? RoomName { get; set; }
    public string TeacherName { get; set; } = string.Empty;
}

public class ParentAttendanceDto
{
    public int AttendanceId { get; set; }
    public int LessonId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string LessonTitle { get; set; } = string.Empty;
    public DateOnly LessonDate { get; set; }
    public string Status { get; set; } = string.Empty; // Present, Absent, Excused, Late
    public string? Note { get; set; }
}

public class ParentGradeDto
{
    public int GradeId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string ExamName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string? Comments { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ParentHomeworkDto
{
    public int HomeworkId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string SubmissionStatus { get; set; } = string.Empty; // Submitted, NotSubmitted, Graded
    public decimal? Score { get; set; }
    public string? TeacherFeedback { get; set; }
}

public class ParentInvoiceDto
{
    public int InvoiceId { get; set; }
    public string InvoiceCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = string.Empty; // Paid, Unpaid, Partial
}

public class ParentNotificationDto
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ParentChatConversationDto
{
    public int TeacherUserId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
}

public class ParentChatMessageDto
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}
