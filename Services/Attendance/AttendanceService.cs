using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherAttendance;

namespace EduBridge.Services.Attendance
{
    public sealed class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;

        public AttendanceService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<LessonDropdownDto>> GetLessonsByClassAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<LessonDropdownDto>();

            var isAssigned = await _context.Classes.AnyAsync(
                c => c.ClassId == classId && c.TeacherId == teacher.TeacherId && !c.IsDeleted, 
                cancellationToken);
            if (!isAssigned) return new List<LessonDropdownDto>();

            var lessons = await _context.Lessons
                .Where(l => l.ClassId == classId)
                .OrderByDescending(l => l.LessonDate)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);

            return lessons.Select(l => new LessonDropdownDto
            {
                LessonId = l.LessonId,
                LessonTitle = l.LessonTitle,
                DateString = l.LessonDate.ToString("dd/MM/yyyy")
            }).ToList();
        }

        public async Task<List<StudentAttendanceDto>> GetAttendanceByLessonAsync(int teacherUserId, int lessonId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<StudentAttendanceDto>();

            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId, cancellationToken);

            if (lesson == null || lesson.Class.TeacherId != teacher.TeacherId || lesson.Class.IsDeleted)
            {
                return new List<StudentAttendanceDto>();
            }

            var activeStudents = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.ClassId == lesson.ClassId && e.Status == "Đang học" && !e.Student.IsDeleted)
                .Select(e => e.Student)
                .OrderBy(s => s.FullName)
                .ToListAsync(cancellationToken);

            var studentIds = activeStudents.Select(s => s.StudentId).ToList();

            var attendances = await _context.Attendances
                .Where(a => a.LessonId == lessonId && studentIds.Contains(a.StudentId))
                .ToDictionaryAsync(a => a.StudentId, cancellationToken);

            var result = new List<StudentAttendanceDto>();
            foreach (var student in activeStudents)
            {
                attendances.TryGetValue(student.StudentId, out var att);
                
                // Ánh xạ trạng thái từ DB (tiếng Việt) sang DTO (tiếng Anh)
                string? mappedStatus = att?.Status switch
                {
                    "Có mặt" => "Present",
                    "Muộn" => "Late",
                    "Vắng" => "Absent",
                    _ => att?.Status
                };

                result.Add(new StudentAttendanceDto
                {
                    StudentId = student.StudentId,
                    StudentName = student.FullName,
                    StudentCode = student.StudentCode,
                    Status = mappedStatus,
                    Note = att?.Note
                });
            }

            return result;
        }

        public async Task<bool> SaveAttendanceAsync(int teacherUserId, SaveAttendanceRequest request, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return false;

            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.LessonId == request.LessonId, cancellationToken);

            if (lesson == null || lesson.Class.TeacherId != teacher.TeacherId || lesson.Class.IsDeleted)
            {
                return false;
            }

            var studentIdsInClass = await _context.Enrollments
                .Where(e => e.ClassId == lesson.ClassId && e.Status == "Đang học" && !e.Student.IsDeleted)
                .Select(e => e.StudentId)
                .ToListAsync(cancellationToken);

            var existingAttendances = await _context.Attendances
                .Where(a => a.LessonId == request.LessonId)
                .ToListAsync(cancellationToken);

            foreach (var input in request.Attendances)
            {
                // Chỉ điểm danh học sinh thuộc lớp
                if (!studentIdsInClass.Contains(input.StudentId)) continue;

                // Ánh xạ trạng thái từ DTO (tiếng Anh) sang DB (tiếng Việt) để tuân thủ CHECK CONSTRAINT
                string dbStatus = input.Status switch
                {
                    "Present" => "Có mặt",
                    "Late" => "Muộn",
                    "Absent" => "Vắng",
                    _ => "Có mặt"
                };

                var att = existingAttendances.FirstOrDefault(a => a.StudentId == input.StudentId);
                if (att == null)
                {
                    att = new Models.Attendance
                    {
                        LessonId = request.LessonId,
                        StudentId = input.StudentId,
                        Status = dbStatus,
                        Note = input.Note?.Trim(),
                        RecordedAt = DateTime.Now
                    };
                    _context.Attendances.Add(att);
                }
                else
                {
                    att.Status = dbStatus;
                    att.Note = input.Note?.Trim();
                    att.RecordedAt = DateTime.Now;
                    _context.Attendances.Update(att);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<AttendanceHistoryDto>> GetAttendanceHistoryAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<AttendanceHistoryDto>();

            var classObj = await _context.Classes.FirstOrDefaultAsync(
                c => c.ClassId == classId && c.TeacherId == teacher.TeacherId && !c.IsDeleted, 
                cancellationToken);
            if (classObj == null) return new List<AttendanceHistoryDto>();

            var lessons = await _context.Lessons
                .Where(l => l.ClassId == classId)
                .OrderByDescending(l => l.LessonDate)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);

            var lessonIds = lessons.Select(l => l.LessonId).ToList();

            var attendances = await _context.Attendances
                .Where(a => lessonIds.Contains(a.LessonId))
                .ToListAsync(cancellationToken);

            var result = new List<AttendanceHistoryDto>();
            foreach (var lesson in lessons)
            {
                var lessonAtts = attendances.Where(a => a.LessonId == lesson.LessonId).ToList();
                
                // Đếm thống kê theo giá trị tiếng Việt thực tế trong DB
                int present = lessonAtts.Count(a => string.Equals(a.Status, "Có mặt", StringComparison.OrdinalIgnoreCase));
                int absent = lessonAtts.Count(a => string.Equals(a.Status, "Vắng", StringComparison.OrdinalIgnoreCase));
                int late = lessonAtts.Count(a => string.Equals(a.Status, "Muộn", StringComparison.OrdinalIgnoreCase));
                int total = lessonAtts.Count;

                string rate = "0%";
                if (total > 0)
                {
                    double percent = Math.Round((double)(present + late) / total * 100);
                    rate = $"{percent}%";
                }

                result.Add(new AttendanceHistoryDto
                {
                    LessonId = lesson.LessonId,
                    DateString = lesson.LessonDate.ToString("dd/MM/yyyy"),
                    ClassName = classObj.ClassName,
                    PresentCount = present,
                    AbsentCount = absent,
                    LateCount = late,
                    AttendanceRate = rate
                });
            }

            return result;
        }
    }
}
