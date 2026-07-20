using System.ComponentModel.DataAnnotations;

namespace EduBridge.Contracts.Classes;

public sealed class EnrollStudentRequest
{
    [MinLength(1)]
    public List<int> StudentIds { get; set; } = new();

    [MaxLength(500)]
    public string? Note { get; set; }
}
