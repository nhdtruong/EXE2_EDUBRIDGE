using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherGrades;

namespace EduBridge.Services.Grades
{
    public sealed class GradeService : IGradeService
    {
        private readonly AppDbContext _context;

        public GradeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TeacherClassDto>> GetTeacherClassesAsync(int teacherUserId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<TeacherClassDto>();

            var classes = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active" && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            return classes.Select(c => new TeacherClassDto
            {
                ClassId = c.ClassId,
                ClassName = c.ClassName
            }).ToList();
        }

        public async Task<List<StudentGradesDto>> GetGradesByClassAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<StudentGradesDto>();

            var classObj = await _context.Classes.FirstOrDefaultAsync(
                c => c.ClassId == classId && c.TeacherId == teacher.TeacherId && !c.IsDeleted, 
                cancellationToken);
            if (classObj == null) return new List<StudentGradesDto>();

            var activeStudents = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.ClassId == classId && e.Status == "Đang học" && !e.Student.IsDeleted)
                .Select(e => e.Student)
                .OrderBy(s => s.FullName)
                .ToListAsync(cancellationToken);

            var studentIds = activeStudents.Select(s => s.StudentId).ToList();

            var grades = await _context.Grades
                .Where(g => g.ClassId == classId && studentIds.Contains(g.StudentId))
                .ToListAsync(cancellationToken);

            var result = new List<StudentGradesDto>();

            foreach (var student in activeStudents)
            {
                var studentGrades = grades.Where(g => g.StudentId == student.StudentId).ToList();

                var kt1Grade = studentGrades.FirstOrDefault(g => IsKT1(g.GradeName));
                var kt2Grade = studentGrades.FirstOrDefault(g => IsKT2(g.GradeName));
                var midtermGrade = studentGrades.FirstOrDefault(g => IsMidterm(g.GradeName));
                var finalGrade = studentGrades.FirstOrDefault(g => IsFinal(g.GradeName));

                var scores = new List<decimal>();
                if (kt1Grade != null) scores.Add(kt1Grade.Score);
                if (kt2Grade != null) scores.Add(kt2Grade.Score);
                if (midtermGrade != null) scores.Add(midtermGrade.Score);
                if (finalGrade != null) scores.Add(finalGrade.Score);

                decimal? averageScore = null;
                if (scores.Count > 0)
                {
                    averageScore = Math.Round(scores.Average(), 1);
                }

                result.Add(new StudentGradesDto
                {
                    StudentId = student.StudentId,
                    StudentName = student.FullName,
                    StudentCode = student.StudentCode,
                    ScoreKT1 = kt1Grade?.Score,
                    CommentKT1 = kt1Grade?.Comment,
                    ScoreKT2 = kt2Grade?.Score,
                    CommentKT2 = kt2Grade?.Comment,
                    ScoreMidterm = midtermGrade?.Score,
                    CommentMidterm = midtermGrade?.Comment,
                    ScoreFinal = finalGrade?.Score,
                    CommentFinal = finalGrade?.Comment,
                    AverageScore = averageScore
                });
            }

            return result;
        }

        public async Task<bool> SaveStudentGradesAsync(int teacherUserId, SaveStudentGradesRequest request, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return false;

            var isAssigned = await _context.Classes.AnyAsync(
                c => c.ClassId == request.ClassId && c.TeacherId == teacher.TeacherId && !c.IsDeleted, 
                cancellationToken);
            if (!isAssigned) return false;

            var isEnrolled = await _context.Enrollments.AnyAsync(
                e => e.ClassId == request.ClassId && e.StudentId == request.StudentId && e.Status == "Đang học", 
                cancellationToken);
            if (!isEnrolled) return false;

            await UpdateOrDeleteGrade(request.ClassId, request.StudentId, "KT 1", request.ScoreKT1, request.CommentKT1, cancellationToken);
            await UpdateOrDeleteGrade(request.ClassId, request.StudentId, "KT 2", request.ScoreKT2, request.CommentKT2, cancellationToken);
            await UpdateOrDeleteGrade(request.ClassId, request.StudentId, "Giữa kỳ", request.ScoreMidterm, request.CommentMidterm, cancellationToken);
            await UpdateOrDeleteGrade(request.ClassId, request.StudentId, "Cuối kỳ", request.ScoreFinal, request.CommentFinal, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task UpdateOrDeleteGrade(int classId, int studentId, string gradeName, decimal? score, string? comment, CancellationToken cancellationToken)
        {
            var grade = await _context.Grades.FirstOrDefaultAsync(
                g => g.ClassId == classId && g.StudentId == studentId && 
                     (g.GradeName == gradeName || 
                      (gradeName == "KT 1" && g.GradeName == "KT1") ||
                      (gradeName == "KT 2" && g.GradeName == "KT2") ||
                      (gradeName == "Giữa kỳ" && g.GradeName == "Giữa kì") ||
                      (gradeName == "Cuối kỳ" && g.GradeName == "Cuối kì")), 
                cancellationToken);

            if (score.HasValue)
            {
                if (grade == null)
                {
                    grade = new Grade
                    {
                        ClassId = classId,
                        StudentId = studentId,
                        GradeName = gradeName,
                        Score = score.Value,
                        Comment = comment?.Trim(),
                        CreatedAt = EduBridge.Helpers.TimeHelper.GetVietnamNow()
                    };
                    _context.Grades.Add(grade);
                }
                else
                {
                    grade.GradeName = gradeName; // Chuẩn hóa lại tên
                    grade.Score = score.Value;
                    grade.Comment = comment?.Trim();
                    _context.Grades.Update(grade);
                }
            }
            else
            {
                if (grade != null)
                {
                    _context.Grades.Remove(grade);
                }
            }
        }

        private bool IsKT1(string name) => 
            string.Equals(name, "KT 1", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(name, "KT1", StringComparison.OrdinalIgnoreCase);

        private bool IsKT2(string name) => 
            string.Equals(name, "KT 2", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(name, "KT2", StringComparison.OrdinalIgnoreCase);

        private bool IsMidterm(string name) => 
            string.Equals(name, "Giữa kỳ", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(name, "Giữa kì", StringComparison.OrdinalIgnoreCase);

        private bool IsFinal(string name) => 
            string.Equals(name, "Cuối kỳ", StringComparison.OrdinalIgnoreCase) || 
            string.Equals(name, "Cuối kì", StringComparison.OrdinalIgnoreCase);
    }
}
