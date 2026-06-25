using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class Homework
{
    public int HomeworkId { get; set; }

    public int LessonId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? AttachmentUrl { get; set; }

    public virtual ICollection<HomeworkSubmission> HomeworkSubmissions { get; set; } = new List<HomeworkSubmission>();

    public virtual Lesson Lesson { get; set; } = null!;
}
