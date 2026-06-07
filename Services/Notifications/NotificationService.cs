using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherNotification;
using EduBridge.Hubs;

namespace EduBridge.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<TeacherClassDto>> GetTeacherClassesAsync(int teacherUserId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<TeacherClassDto>();

            var classes = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active" && !c.IsDeleted)
                .Select(c => new TeacherClassDto
                {
                    ClassId = c.ClassId,
                    ClassName = c.ClassName
                })
                .ToListAsync(cancellationToken);

            return classes;
        }

        public async Task<bool> BroadcastNotificationAsync(int teacherUserId, BroadcastNotificationRequest request, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return false;

            // Xác thực xem lớp học có thuộc quyền quản lý của Giáo viên hay không
            var isTeacherClass = await _context.Classes
                .AnyAsync(c => c.ClassId == request.ClassId && c.TeacherId == teacher.TeacherId && c.Status == "Active" && !c.IsDeleted, cancellationToken);
            if (!isTeacherClass) return false;

            // Lấy danh sách phụ huynh của các học sinh thuộc lớp học này
            var parentUserIds = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.ClassId == request.ClassId && e.Status == "Đang học" && !e.Student.IsDeleted)
                .Select(e => e.Student.ParentUserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!parentUserIds.Any()) return true; // Lớp không có học sinh/phụ huynh nào thì vẫn xem như gửi xong

            var notifications = new List<Notification>();
            var now = DateTime.Now;

            foreach (var parentId in parentUserIds)
            {
                var notification = new Notification
                {
                    UserId = parentId,
                    Title = request.Title.Trim(),
                    Content = request.Content.Trim(),
                    IsRead = false,
                    CreatedAt = now
                };
                notifications.Add(notification);
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync(cancellationToken);

            // Gửi realtime qua SignalR Hub tới từng phụ huynh đang hoạt động
            foreach (var notif in notifications)
            {
                var payload = new
                {
                    notificationId = notif.NotificationId,
                    title = notif.Title,
                    content = notif.Content,
                    isRead = notif.IsRead,
                    createdAt = notif.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                };

                // Nhóm của user trong ChatHub là: user-{userId}
                await _hubContext.Clients.Group($"user-{notif.UserId}")
                    .SendAsync("ReceiveNotification", payload);
            }

            return true;
        }
    }
}
