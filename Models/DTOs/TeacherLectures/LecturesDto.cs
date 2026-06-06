using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EduBridge.Models.DTOs.TeacherLectures
{
    public class LecturesResponseDto
    {
        public List<ClassProgressDto> ClassProgresses { get; set; } = new();
        public List<LectureHistoryDto> LectureHistories { get; set; } = new();
    }

    public class ClassProgressDto
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
        public int PercentComplete => TotalLessons == 0 ? 0 : (int)((double)CompletedLessons / TotalLessons * 100);
    }

    public class LectureHistoryDto
    {
        public int LessonId { get; set; }
        public string DateString { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; 
        public DateTime CreatedAt { get; set; }
    }

    public class AddLectureNoteRequest
    {
        [Required]
        public int ClassId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Topic { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
    }
}
