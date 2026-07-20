using System;

namespace EduBridge.Models.DTOs.Shared
{
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedAtString => CreatedAt.ToString("dd/MM/yyyy HH:mm");
    }
}
