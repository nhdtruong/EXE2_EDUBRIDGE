namespace EduBridge.Contracts.Students;

public class StudentQuery
{
    public string? Keyword { get; set; }
    public string? ParentKeyword { get; set; }
    public string? ContactKeyword { get; set; }
    public string? Gender { get; set; }
    public int? ClassId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public void Normalize()
    {
        Keyword = NormalizeFilterValue(Keyword);
        ParentKeyword = NormalizeFilterValue(ParentKeyword);
        ContactKeyword = NormalizeFilterValue(ContactKeyword);
        Gender = NormalizeFilterValue(Gender);
        Status = NormalizeFilterValue(Status);

        if (PageSize <= 0) PageSize = 20;
        if (PageNumber < 1) PageNumber = 1;
    }

    private static string? NormalizeFilterValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
    }
}
