using EduBridge.Data;
using EduBridge.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace EduBridge.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public async Task SendMessage(int conversationId, int senderId, string content)
        {
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                var message = new Message
                {
                    ConversationId = conversationId,
                    SenderId = senderId,
                    Content = content,
                    SentAt = DateTime.Now
                };

                _context.Messages.Add(message);
                
                conversation.LastMessage = content;
                conversation.LastMessageTime = message.SentAt;
                
                await _context.SaveChangesAsync();

                // Broadcast to all connected clients for simplicity 
                // In production, we would map Context.ConnectionId to Users or Groups
                await Clients.All.SendAsync("ReceiveMessage", conversationId, senderId, content, message.SentAt.ToString("HH:mm"));
            }
        }
    }
}
