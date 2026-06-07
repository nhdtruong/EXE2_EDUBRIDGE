using System;
using System.ComponentModel.DataAnnotations;

namespace EduBridge.Models.DTOs.TeacherGrades
{
    public class StudentGradesDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        
        // Điểm KT 1
        public decimal? ScoreKT1 { get; set; }
        public string? CommentKT1 { get; set; }

        // Điểm KT 2
        public decimal? ScoreKT2 { get; set; }
        public string? CommentKT2 { get; set; }

        // Điểm Giữa kỳ
        public decimal? ScoreMidterm { get; set; }
        public string? CommentMidterm { get; set; }

        // Điểm Cuối kỳ
        public decimal? ScoreFinal { get; set; }
        public string? CommentFinal { get; set; }

        // Điểm trung bình (TB)
        public decimal? AverageScore { get; set; }
    }

    public class SaveStudentGradesRequest
    {
        [Required(ErrorMessage = "Lớp học là bắt buộc.")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Học sinh là bắt buộc.")]
        public int StudentId { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm số phải từ 0 đến 10.")]
        public decimal? ScoreKT1 { get; set; }
        public string? CommentKT1 { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm số phải từ 0 đến 10.")]
        public decimal? ScoreKT2 { get; set; }
        public string? CommentKT2 { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm số phải từ 0 đến 10.")]
        public decimal? ScoreMidterm { get; set; }
        public string? CommentMidterm { get; set; }

        [Range(0, 10, ErrorMessage = "Điểm số phải từ 0 đến 10.")]
        public decimal? ScoreFinal { get; set; }
        public string? CommentFinal { get; set; }
    }
}
