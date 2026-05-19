using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace EduBridge.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        
        public int TeacherUserId { get; set; }
        public User Teacher { get; set; }
        
        public int ParentUserId { get; set; }
        public User Parent { get; set; }
        
        public string StudentName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
        
        public List<Message> Messages { get; set; } = new List<Message>();
    }
}
