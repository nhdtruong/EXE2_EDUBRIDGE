namespace EduBridge.Contracts.Courses;

public class CourseQuery
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
