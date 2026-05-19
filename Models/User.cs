using System.ComponentModel.DataAnnotations;

namespace EduBridge.Models
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // "Teacher", "Parent", "Admin"
    }
}
