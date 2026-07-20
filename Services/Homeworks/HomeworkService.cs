using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherHomework;

namespace EduBridge.Services.Homeworks
{
    public sealed class HomeworkService : IHomeworkService
    {
        private readonly AppDbContext _context;

        public HomeworkService(AppDbContext context)
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

        public async Task<List<HomeworkListItemDto>> GetHomeworkListAsync(int teacherUserId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<HomeworkListItemDto>();

            var teacherClasses = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active" && !c.IsDeleted)
                .Select(c => c.ClassId)
                .ToListAsync(cancellationToken);

            var homeworks = await _context.Homeworks
                .Include(h => h.Lesson)
                    .ThenInclude(l => l.Class)
                .Where(h => teacherClasses.Contains(h.Lesson.ClassId) && !h.Lesson.Class.IsDeleted)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(cancellationToken);

            var result = new List<HomeworkListItemDto>();

            foreach (var h in homeworks)
            {
                var totalStudents = await _context.Enrollments
                    .CountAsync(e => e.ClassId == h.Lesson.ClassId && e.Status == "Đang học" && !e.Student.IsDeleted, cancellationToken);

                var submissions = await _context.HomeworkSubmissions
                    .Where(s => s.HomeworkId == h.HomeworkId)
                    .ToListAsync(cancellationToken);

                var submittedCount = submissions.Count;
                var gradedCount = submissions.Count(s => s.Status == "Graded");
                var pendingCount = submittedCount - gradedCount;

                result.Add(new HomeworkListItemDto
                {
                    HomeworkId = h.HomeworkId,
                    LessonId = h.LessonId,
                    ClassName = h.Lesson.Class.ClassName,
                    Title = h.Title,
                    Description = h.Description ?? string.Empty,
                    CreatedAtString = h.CreatedAt.ToString("dd/MM/yyyy"),
                    DueDateString = h.DueDate.HasValue ? h.DueDate.Value.ToString("dd/MM/yyyy HH:mm") : "Không có hạn",
                    AttachmentUrl = h.AttachmentUrl,
                    SubmittedCount = submittedCount,
                    TotalStudents = totalStudents,
                    GradedCount = gradedCount,
                    PendingCount = pendingCount
                });
            }

            return result;
        }

        public async Task<bool> CreateHomeworkAsync(int teacherUserId, CreateHomeworkRequest request, string? attachmentUrl, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return false;

            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.LessonId == request.LessonId, cancellationToken);

            if (lesson == null || lesson.Class.TeacherId != teacher.TeacherId || lesson.Class.IsDeleted || lesson.Class.Status != "Active")
            {
                return false;
            }

            var homework = new Homework
            {
                LessonId = request.LessonId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                DueDate = request.DueDate,
                AttachmentUrl = attachmentUrl,
                CreatedAt = EduBridge.Helpers.TimeHelper.GetVietnamNow()
            };

            _context.Homeworks.Add(homework);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<List<HomeworkSubmissionListItemDto>> GetSubmissionsAsync(int teacherUserId, int homeworkId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<HomeworkSubmissionListItemDto>();

            var homework = await _context.Homeworks
                .Include(h => h.Lesson)
                    .ThenInclude(l => l.Class)
                .FirstOrDefaultAsync(h => h.HomeworkId == homeworkId, cancellationToken);

            if (homework == null || homework.Lesson.Class.TeacherId != teacher.TeacherId || homework.Lesson.Class.IsDeleted)
            {
                return new List<HomeworkSubmissionListItemDto>();
            }

            var activeStudents = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.ClassId == homework.Lesson.ClassId && e.Status == "Đang học" && !e.Student.IsDeleted)
                .Select(e => e.Student)
                .OrderBy(s => s.FullName)
                .ToListAsync(cancellationToken);

            var submissions = await _context.HomeworkSubmissions
                .Where(s => s.HomeworkId == homeworkId)
                .ToDictionaryAsync(s => s.StudentId, cancellationToken);

            var result = new List<HomeworkSubmissionListItemDto>();

            foreach (var student in activeStudents)
            {
                if (submissions.TryGetValue(student.StudentId, out var sub))
                {
                    result.Add(new HomeworkSubmissionListItemDto
                    {
                        StudentId = student.StudentId,
                        StudentName = student.FullName,
                        StudentCode = student.StudentCode,
                        SubmissionId = sub.SubmissionId,
                        SubmissionContent = sub.SubmissionContent,
                        SubmissionFileUrl = sub.SubmissionFileUrl,
                        Status = sub.Status,
                        Score = sub.Score,
                        Feedback = sub.Feedback,
                        SubmittedAtString = sub.SubmittedAt.ToString("dd/MM/yyyy HH:mm")
                    });
                }
                else
                {
                    result.Add(new HomeworkSubmissionListItemDto
                    {
                        StudentId = student.StudentId,
                        StudentName = student.FullName,
                        StudentCode = student.StudentCode,
                        SubmissionId = null,
                        SubmissionContent = null,
                        SubmissionFileUrl = null,
                        Status = "NotSubmitted",
                        Score = null,
                        Feedback = null,
                        SubmittedAtString = null
                    });
                }
            }

            return result;
        }

        public async Task<bool> GradeSubmissionAsync(int teacherUserId, int homeworkId, int studentId, GradeSubmissionRequest request, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return false;

            var homework = await _context.Homeworks
                .Include(h => h.Lesson)
                    .ThenInclude(l => l.Class)
                .FirstOrDefaultAsync(h => h.HomeworkId == homeworkId, cancellationToken);

            if (homework == null || homework.Lesson.Class.TeacherId != teacher.TeacherId || homework.Lesson.Class.IsDeleted)
            {
                return false;
            }

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.ClassId == homework.Lesson.ClassId && e.StudentId == studentId && e.Status == "Đang học", cancellationToken);
            if (!isEnrolled) return false;

            var submission = await _context.HomeworkSubmissions
                .FirstOrDefaultAsync(s => s.HomeworkId == homeworkId && s.StudentId == studentId, cancellationToken);

            if (submission == null)
            {
                // Học sinh chưa nộp bài, nhưng giáo viên chấm điểm trực tiếp
                submission = new HomeworkSubmission
                {
                    HomeworkId = homeworkId,
                    StudentId = studentId,
                    SubmissionContent = "Giáo viên chủ động chấm điểm trực tiếp (không thông qua bài nộp hệ thống).",
                    SubmittedAt = EduBridge.Helpers.TimeHelper.GetVietnamNow(),
                    Score = request.Score,
                    Feedback = request.Feedback?.Trim(),
                    Status = "Graded"
                };
                _context.HomeworkSubmissions.Add(submission);
            }
            else
            {
                submission.Score = request.Score;
                submission.Feedback = request.Feedback?.Trim();
                submission.Status = "Graded";
                _context.HomeworkSubmissions.Update(submission);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<LessonDropdownOptionDto>> GetLessonsByClassAsync(int teacherUserId, int classId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<LessonDropdownOptionDto>();

            var isAssigned = await _context.Classes
                .AnyAsync(c => c.ClassId == classId && c.TeacherId == teacher.TeacherId && !c.IsDeleted, cancellationToken);
            if (!isAssigned) return new List<LessonDropdownOptionDto>();

            var lessons = await _context.Lessons
                .Where(l => l.ClassId == classId)
                .OrderByDescending(l => l.LessonDate)
                .ThenByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);

            return lessons.Select(l => new LessonDropdownOptionDto
            {
                LessonId = l.LessonId,
                LessonTitle = l.LessonTitle,
                DateString = l.LessonDate.ToString("dd/MM/yyyy")
            }).ToList();
        }

        public async Task<List<ParentHomeworkItemDto>> GetParentHomeworksAsync(int parentUserId, CancellationToken cancellationToken = default)
        {
            var students = await _context.Students
                .Where(s => s.ParentUserId == parentUserId && !s.IsDeleted && s.Status == "Active")
                .ToListAsync(cancellationToken);

            if (!students.Any()) return new List<ParentHomeworkItemDto>();

            var studentIds = students.Select(s => s.StudentId).ToList();

            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                .Where(e => studentIds.Contains(e.StudentId) && e.Status == "Đang học" && !e.Class.IsDeleted && e.Class.Status == "Active")
                .ToListAsync(cancellationToken);

            if (!enrollments.Any()) return new List<ParentHomeworkItemDto>();

            var classIds = enrollments.Select(e => e.ClassId).Distinct().ToList();

            var homeworks = await _context.Homeworks
                .Include(h => h.Lesson)
                    .ThenInclude(l => l.Class)
                .Where(h => classIds.Contains(h.Lesson.ClassId))
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync(cancellationToken);

            var homeworkIds = homeworks.Select(h => h.HomeworkId).ToList();

            var submissions = await _context.HomeworkSubmissions
                .Where(s => homeworkIds.Contains(s.HomeworkId) && studentIds.Contains(s.StudentId))
                .ToListAsync(cancellationToken);

            var result = new List<ParentHomeworkItemDto>();
            var now = EduBridge.Helpers.TimeHelper.GetVietnamNow();

            foreach (var enrollment in enrollments)
            {
                var student = students.First(s => s.StudentId == enrollment.StudentId);
                var studentClassHomeworks = homeworks.Where(h => h.Lesson.ClassId == enrollment.ClassId);

                foreach (var h in studentClassHomeworks)
                {
                    var sub = submissions.FirstOrDefault(s => s.HomeworkId == h.HomeworkId && s.StudentId == student.StudentId);

                    string status = "NotSubmitted";
                    if (sub != null)
                    {
                        status = sub.Status; 
                    }
                    else if (h.DueDate.HasValue && h.DueDate.Value < now)
                    {
                        status = "Overdue";
                    }

                    result.Add(new ParentHomeworkItemDto
                    {
                        HomeworkId = h.HomeworkId,
                        Title = h.Title,
                        Description = h.Description ?? string.Empty,
                        ClassName = h.Lesson.Class.ClassName,
                        LessonTitle = h.Lesson.LessonTitle,
                        AttachmentUrl = h.AttachmentUrl,
                        DueDate = h.DueDate,
                        DueDateString = h.DueDate.HasValue ? h.DueDate.Value.ToString("dd/MM/yyyy HH:mm") : "Không có hạn",
                        StudentId = student.StudentId,
                        StudentName = student.FullName,
                        SubmissionId = sub?.SubmissionId,
                        SubmissionContent = sub?.SubmissionContent,
                        SubmissionFileUrl = sub?.SubmissionFileUrl,
                        SubmittedAtString = sub?.SubmittedAt.ToString("dd/MM/yyyy HH:mm"),
                        Status = status,
                        Score = sub?.Score,
                        Feedback = sub?.Feedback
                    });
                }
            }

            return result.OrderByDescending(r => r.HomeworkId).ToList();
        }

        public async Task<bool> SubmitHomeworkAsync(int parentUserId, SubmitHomeworkRequestDto request, CancellationToken cancellationToken = default)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == request.StudentId && s.ParentUserId == parentUserId && !s.IsDeleted && s.Status == "Active", cancellationToken);
            if (student == null) return false;

            var homework = await _context.Homeworks
                .Include(h => h.Lesson)
                .FirstOrDefaultAsync(h => h.HomeworkId == request.HomeworkId, cancellationToken);
            if (homework == null) return false;

            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.ClassId == homework.Lesson.ClassId && e.StudentId == request.StudentId && e.Status == "Đang học", cancellationToken);
            if (!isEnrolled) return false;

            var now = EduBridge.Helpers.TimeHelper.GetVietnamNow();
            if (homework.DueDate.HasValue && homework.DueDate.Value < now)
            {
                return false;
            }

            var submission = await _context.HomeworkSubmissions
                .FirstOrDefaultAsync(s => s.HomeworkId == request.HomeworkId && s.StudentId == request.StudentId, cancellationToken);

            if (submission != null)
            {
                if (submission.Status == "Graded")
                {
                    return false;
                }

                submission.SubmissionContent = request.SubmissionContent?.Trim();
                submission.SubmissionFileUrl = request.SubmissionFileUrl.Trim();
                submission.SubmittedAt = now;
                submission.Status = "Submitted";

                _context.HomeworkSubmissions.Update(submission);
            }
            else
            {
                submission = new HomeworkSubmission
                {
                    HomeworkId = request.HomeworkId,
                    StudentId = request.StudentId,
                    SubmissionContent = request.SubmissionContent?.Trim(),
                    SubmissionFileUrl = request.SubmissionFileUrl.Trim(),
                    SubmittedAt = now,
                    Status = "Submitted"
                };

                _context.HomeworkSubmissions.Add(submission);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
