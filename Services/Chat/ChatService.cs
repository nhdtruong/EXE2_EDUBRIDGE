using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherChat;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _context;

        public ChatService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ConversationDto>> GetTeacherConversationsAsync(int teacherUserId, CancellationToken cancellationToken = default)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == teacherUserId, cancellationToken);
            if (teacher == null) return new List<ConversationDto>();

            // Lấy tất cả lớp học đang hoạt động do giáo viên này phụ trách
            var classIds = await _context.Classes
                .Where(c => c.TeacherId == teacher.TeacherId && c.Status == "Active" && !c.IsDeleted)
                .Select(c => c.ClassId)
                .ToListAsync(cancellationToken);

            if (!classIds.Any()) return new List<ConversationDto>();

            // Lấy danh sách học sinh đang học trong các lớp đó và thông tin Phụ huynh (chỉ lấy học sinh đã được gán tài khoản phụ huynh)
            var studentsData = await _context.Enrollments
                .Include(e => e.Student)
                .ThenInclude(s => s.ParentUser)
                .Where(e => classIds.Contains(e.ClassId) && e.Status == "Đang học" && !e.Student.IsDeleted && e.Student.ParentUserId != null)
                .Select(e => new
                {
                    ParentUserId = e.Student.ParentUserId!.Value,
                    ParentName = e.Student.ParentUser != null ? e.Student.ParentUser.FullName : string.Empty,
                    StudentName = e.Student.FullName
                })
                .ToListAsync(cancellationToken);

            // Nhóm theo phụ huynh
            var groupedByParent = studentsData
                .GroupBy(s => s.ParentUserId)
                .ToList();

            var parentUserIds = groupedByParent.Select(g => g.Key).ToList();

            // Tải tất cả tin nhắn liên quan để xử lý in-memory tối ưu hóa hiệu năng
            var allMessages = await _context.Messages
                .Where(m => (m.SenderUserId == teacherUserId && parentUserIds.Contains(m.ReceiverUserId)) ||
                            (m.ReceiverUserId == teacherUserId && parentUserIds.Contains(m.SenderUserId)))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync(cancellationToken);

            var conversations = groupedByParent.Select(group =>
            {
                var parentId = group.Key;

                // Lấy các tin nhắn giữa giáo viên và phụ huynh này
                var parentMessages = allMessages
                    .Where(m => (m.SenderUserId == teacherUserId && m.ReceiverUserId == parentId) ||
                                (m.SenderUserId == parentId && m.ReceiverUserId == teacherUserId))
                    .ToList();

                var lastMsg = parentMessages.FirstOrDefault();

                // Đếm tin nhắn chưa đọc mà phụ huynh gửi cho giáo viên
                var unreadCount = parentMessages
                    .Count(m => m.SenderUserId == parentId && m.ReceiverUserId == teacherUserId && !m.IsRead);

                return new
                {
                    ParentUserId = parentId,
                    ParentName = group.First().ParentName,
                    StudentNames = string.Join(", ", group.Select(s => s.StudentName).Distinct()),
                    LastMessage = lastMsg?.Content,
                    LastMessageSenderId = lastMsg?.SenderUserId,
                    LastMsgAt = lastMsg?.SentAt,
                    UnreadCount = unreadCount
                };
            })
            .OrderByDescending(c => c.LastMsgAt.HasValue)
            .ThenByDescending(c => c.LastMsgAt)
            .ThenBy(c => c.ParentName)
            .Select(c => new ConversationDto
            {
                ParentUserId = c.ParentUserId,
                ParentName = c.ParentName,
                StudentNames = c.StudentNames,
                LastMessage = c.LastMessage,
                LastMessageSenderId = c.LastMessageSenderId,
                LastMessageTime = c.LastMsgAt?.ToString("dd/MM/yyyy HH:mm"),
                UnreadCount = c.UnreadCount
            })
            .ToList();

            return conversations;
        }

        public async Task<List<ConversationDto>> GetParentConversationsAsync(int parentUserId, CancellationToken cancellationToken = default)
        {
            // Lấy danh sách các học sinh thuộc phụ huynh này
            var students = await _context.Students
                .Where(s => s.ParentUserId == parentUserId && !s.IsDeleted)
                .ToListAsync(cancellationToken);

            if (!students.Any()) return new List<ConversationDto>();

            var studentIds = students.Select(s => s.StudentId).ToList();

            // Lấy các lớp học đang học của các học sinh này
            var enrollments = await _context.Enrollments
                .Include(e => e.Class)
                .ThenInclude(c => c.Teacher)
                .ThenInclude(t => t.User)
                .Where(e => studentIds.Contains(e.StudentId) && e.Status == "Đang học" && !e.Class.IsDeleted && e.Class.Teacher != null)
                .ToListAsync(cancellationToken);

            if (!enrollments.Any()) return new List<ConversationDto>();

            // Nhóm theo Giáo viên
            var groupedByTeacher = enrollments
                .GroupBy(e => new { e.Class.Teacher.UserId, TeacherName = e.Class.Teacher.User.FullName })
                .ToList();

            var teacherUserIds = groupedByTeacher.Select(g => g.Key.UserId).ToList();

            // Tải tất cả tin nhắn liên quan giữa phụ huynh này và các giáo viên
            var allMessages = await _context.Messages
                .Where(m => (m.SenderUserId == parentUserId && teacherUserIds.Contains(m.ReceiverUserId)) ||
                            (m.ReceiverUserId == parentUserId && teacherUserIds.Contains(m.SenderUserId)))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync(cancellationToken);

            var conversations = groupedByTeacher.Select(group =>
            {
                var teacherUserId = group.Key.UserId;
                var teacherName = group.Key.TeacherName;

                // Danh sách các học sinh của phụ huynh này đang học lớp của giáo viên đó
                var studentNames = string.Join(", ", group
                    .Select(e => students.FirstOrDefault(s => s.StudentId == e.StudentId)?.FullName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct());

                // Tin nhắn giữa phụ huynh và giáo viên này
                var teacherMessages = allMessages
                    .Where(m => (m.SenderUserId == parentUserId && m.ReceiverUserId == teacherUserId) ||
                                (m.SenderUserId == teacherUserId && m.ReceiverUserId == parentUserId))
                    .ToList();

                var lastMsg = teacherMessages.FirstOrDefault();

                // Đếm tin nhắn chưa đọc mà giáo viên gửi cho phụ huynh
                var unreadCount = teacherMessages
                    .Count(m => m.SenderUserId == teacherUserId && m.ReceiverUserId == parentUserId && !m.IsRead);

                return new
                {
                    TeacherUserId = teacherUserId,
                    TeacherName = teacherName,
                    StudentNames = studentNames,
                    LastMessage = lastMsg?.Content,
                    LastMessageSenderId = lastMsg?.SenderUserId,
                    LastMsgAt = lastMsg?.SentAt,
                    UnreadCount = unreadCount
                };
            })
            .OrderByDescending(c => c.LastMsgAt.HasValue)
            .ThenByDescending(c => c.LastMsgAt)
            .ThenBy(c => c.TeacherName)
            .Select(c => new ConversationDto
            {
                ParentUserId = c.TeacherUserId, // Dùng ParentUserId để giữ nguyên cấu trúc DTO cho UI dễ đọc
                ParentName = c.TeacherName,
                StudentNames = c.StudentNames,
                LastMessage = c.LastMessage,
                LastMessageSenderId = c.LastMessageSenderId,
                LastMessageTime = c.LastMsgAt?.ToString("dd/MM/yyyy HH:mm"),
                UnreadCount = c.UnreadCount
            })
            .ToList();

            return conversations;
        }

        public async Task<List<ChatMessageDto>> GetChatHistoryAsync(int currentUserId, int contactUserId, CancellationToken cancellationToken = default)
        {
            var messages = await _context.Messages
                .Where(m => (m.SenderUserId == currentUserId && m.ReceiverUserId == contactUserId) ||
                            (m.SenderUserId == contactUserId && m.ReceiverUserId == currentUserId))
                .OrderBy(m => m.SentAt)
                .ToListAsync(cancellationToken);

            return messages.Select(m => new ChatMessageDto
            {
                MessageId = m.MessageId,
                SenderUserId = m.SenderUserId,
                ReceiverUserId = m.ReceiverUserId,
                Content = m.Content,
                SentAtString = m.SentAt.ToString("dd/MM/yyyy HH:mm"),
                IsRead = m.IsRead,
                IsOutgoing = m.SenderUserId == currentUserId
            }).ToList();
        }

        public async Task<bool> MarkAsReadAsync(int currentUserId, int senderUserId, CancellationToken cancellationToken = default)
        {
            await _context.Messages
                .Where(m => m.SenderUserId == senderUserId && m.ReceiverUserId == currentUserId && !m.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), cancellationToken);

            return true;
        }
    }
}
