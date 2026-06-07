namespace EduBridge.Contracts.Courses;

public class CourseResponse
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int TotalSessions { get; set; }
    public decimal? TuitionFee { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ClassCount { get; set; }
}

public class CoursePagedResponse
{
    public List<CourseResponse> Data { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => TotalItems == 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);
}

public class CourseMutationResponse
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; }
    public string Status { get; set; }

    public CourseMutationResponse(int courseId, string courseCode, string status)
    {
        CourseId = courseId;
        CourseCode = courseCode;
        Status = status;
    }
}
