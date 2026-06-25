using EduBridge.Data;
using EduBridge.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace EduBridge.Hubs
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();

            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    GetUserGroupName(userId.Value));
            }

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(int receiverUserId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return;

            var senderUserId = GetCurrentUserId();

            if (!senderUserId.HasValue)
                return;

            if (senderUserId.Value == receiverUserId)
                return;

            if (!await CanChatAsync(senderUserId.Value, receiverUserId))
                return;

            var message = new Message
            {
                SenderUserId = senderUserId.Value,
                ReceiverUserId = receiverUserId,
                Content = content.Trim(),
                SentAt = EduBridge.Helpers.TimeHelper.GetVietnamNow(),
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var payload = new
            {
                messageId = message.MessageId,
                senderUserId = message.SenderUserId,
                receiverUserId = message.ReceiverUserId,
                content = message.Content,
                sentAt = message.SentAt.ToString("dd/MM/yyyy HH:mm"),
                isRead = message.IsRead
            };

            await Clients.Group(GetUserGroupName(receiverUserId))
                .SendAsync("ReceiveMessage", payload);

            await Clients.Group(GetUserGroupName(senderUserId.Value))
                .SendAsync("ReceiveMessage", payload);
        }

        public async Task MarkAsRead(int senderUserId)
        {
            var receiverUserId = GetCurrentUserId();

            if (!receiverUserId.HasValue)
                return;

            var unreadMessages = await _context.Messages
                .Where(m =>
                    m.SenderUserId == senderUserId &&
                    m.ReceiverUserId == receiverUserId.Value &&
                    !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Count == 0)
                return;

            foreach (var message in unreadMessages)
                message.IsRead = true;

            await _context.SaveChangesAsync();
        }

        private int? GetCurrentUserId()
        {
            var value = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var userId)
                ? userId
                : null;
        }

        private static string GetUserGroupName(int userId)
        {
            return $"user-{userId}";
        }

        private async Task<bool> CanChatAsync(int senderUserId, int receiverUserId)
        {
            var senderRole = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == senderUserId && u.Status == "Active" && !u.IsDeleted)
                .Select(u => u.Role.RoleCode).FirstOrDefaultAsync();
            var receiverRole = await _context.Users.AsNoTracking()
                .Where(u => u.UserId == receiverUserId && u.Status == "Active" && !u.IsDeleted)
                .Select(u => u.Role.RoleCode).FirstOrDefaultAsync();

            if (senderRole == "PARENT" && receiverRole == "TEACHER")
                return await _context.Enrollments.AnyAsync(e =>
                    e.Student.ParentUserId == senderUserId && e.Status == "Đang học" &&
                    e.Class.Teacher != null && e.Class.Teacher.UserId == receiverUserId);

            if (senderRole == "TEACHER" && receiverRole == "PARENT")
                return await _context.Enrollments.AnyAsync(e =>
                    e.Student.ParentUserId == receiverUserId && e.Status == "Đang học" &&
                    e.Class.Teacher != null && e.Class.Teacher.UserId == senderUserId);

            return false;
        }
    }
}
