namespace EduBridge.Pages.AdminStudents;

public sealed class StudentClassOption
{
    public int ClassId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public string ClassCode { get; set; } = string.Empty;

    public string DisplayName => $"{ClassName} ({ClassCode})";
}
