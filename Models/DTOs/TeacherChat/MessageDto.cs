using System;

namespace EduBridge.Models.DTOs.TeacherChat
{
    public class ConversationDto
    {
        public int ParentUserId { get; set; }
        public string ParentName { get; set; } = string.Empty;
        public string StudentNames { get; set; } = string.Empty;
        public string? LastMessage { get; set; }
        public string? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ChatMessageDto
    {
        public int MessageId { get; set; }
        public int SenderUserId { get; set; }
        public int ReceiverUserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SentAtString { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsOutgoing { get; set; }
    }
}
