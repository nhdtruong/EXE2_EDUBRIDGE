using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using EduBridge.Models;
using EduBridge.Models.DTOs.TeacherChat;

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
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderUserId == senderUserId && m.ReceiverUserId == currentUserId && !m.IsRead)
                .ToListAsync(cancellationToken);

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }
            return true;
        }
    }
}
