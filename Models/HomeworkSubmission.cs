using System;
using System.Collections.Generic;

namespace EduBridge.Models;

public partial class HomeworkSubmission
{
    public int SubmissionId { get; set; }

    public int HomeworkId { get; set; }

    public int StudentId { get; set; }

    public string? SubmissionContent { get; set; }

    public DateTime SubmittedAt { get; set; }

    public decimal? Score { get; set; }

    public string? Feedback { get; set; }

    public string Status { get; set; } = null!;

    public virtual Homework Homework { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
