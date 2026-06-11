using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EduBridge.Models.DTOs.TeacherAttendance
{
    public class TeacherClassDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }

    public class LessonDropdownDto
    {
        public int LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string DateString { get; set; } = string.Empty;
    }

    public class StudentAttendanceDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string? Status { get; set; } // Present, Late, Absent, hoặc null nếu chưa điểm danh
        public string? Note { get; set; }
    }

    public class StudentAttendanceInput
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        [RegularExpression("^(Present|Late|Absent)$", ErrorMessage = "Trạng thái điểm danh phải là Present, Late hoặc Absent.")]
        public string Status { get; set; } = string.Empty;

        public string? Note { get; set; }
    }

    public class SaveAttendanceRequest
    {
        [Required]
        public int LessonId { get; set; }

        [Required]
        public List<StudentAttendanceInput> Attendances { get; set; } = new();
    }

    public class AttendanceHistoryDto
    {
        public int LessonId { get; set; }
        public string DateString { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }
        public string AttendanceRate { get; set; } = "0%";
    }
}
