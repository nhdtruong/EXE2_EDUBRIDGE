using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherLectures;

namespace EduBridge.Services.Lectures
{
    public sealed class LectureService : ILectureService
    {
        private readonly AppDbContext _context;

        public LectureService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LecturesResponseDto> GetLecturesDataAsync(int teacherUserId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null)
            {
                return new LecturesResponseDto();
            }

            var teacherClasses = await _context.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active" && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            var classIds = teacherClasses.Select(c => c.ClassId).ToList();

            var classProgresses = new List<ClassProgressDto>();
            foreach (var c in teacherClasses)
            {
                var totalLessons = c.Course?.TotalSessions ?? 20;
                var completedLessons = await _context.Lessons.CountAsync(l => l.ClassId == c.ClassId && l.Status == "Completed", cancellationToken);

                classProgresses.Add(new ClassProgressDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName,
                    CompletedLessons = completedLessons,
                    TotalLessons = totalLessons
                });
            }

            var lessons = await _context.Lessons
                .Include(l => l.Class)
                .Where(l => classIds.Contains(l.ClassId))
                .OrderByDescending(l => l.LessonDate)
                .ThenByDescending(l => l.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);

            var lectureHistories = lessons.Select(l => new LectureHistoryDto
            {
                LessonId = l.LessonId,
                ClassId = l.ClassId,
                DateString = l.LessonDate.ToString("dd/MM/yyyy"),
                ClassName = l.Class.ClassName,
                Topic = l.LessonTitle,
                Content = l.LessonContent ?? "Không có nội dung",
                Status = l.Status, // Sử dụng giá trị thực từ DB
                CreatedAt = l.CreatedAt
            }).ToList();

            return new LecturesResponseDto
            {
                ClassProgresses = classProgresses,
                LectureHistories = lectureHistories
            };
        }

        public async Task<bool> AddLectureNoteAsync(int teacherUserId, AddLectureNoteRequest request, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return false;

            var classExists = await _context.Classes
                .AnyAsync(c => c.ClassId == request.ClassId && c.TeacherId == teacher.TeacherId && !c.IsDeleted && c.Status == "Active", cancellationToken);
            if (!classExists) return false;

            // Kiểm tra các bài giảng hiện tại để tính SessionNumber
            var existingSessionsCount = await _context.Lessons.CountAsync(l => l.ClassId == request.ClassId, cancellationToken);

            var lesson = new Lesson
            {
                ClassId = request.ClassId,
                LessonTitle = request.Topic.Trim(),
                LessonContent = request.Content?.Trim(),
                LessonDate = DateOnly.FromDateTime(DateTime.Now),
                CreatedAt = DateTime.Now,
                SessionNumber = existingSessionsCount + 1,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "Scheduled" : request.Status
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<bool> EditLectureNoteAsync(int teacherUserId, int lessonId, EditLectureNoteRequest request, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return false;

            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId, cancellationToken);

            if (lesson == null) return false;

            // Kiểm tra giáo viên đó có phụ trách lớp của bài giảng này không
            if (lesson.Class.TeacherId != teacher.TeacherId || lesson.Class.IsDeleted) return false;

            lesson.LessonTitle = request.Topic.Trim();
            lesson.LessonContent = request.Content?.Trim();
            lesson.Status = request.Status;

            _context.Lessons.Update(lesson);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
