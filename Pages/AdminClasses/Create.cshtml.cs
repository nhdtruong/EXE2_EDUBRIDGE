using System.Security.Claims;
using EduBridge.Contracts.Classes;
using EduBridge.Data;
using EduBridge.Services.Classes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EduBridge.Pages.AdminClasses;

[Authorize(Policy = "AdminOnly")]
public sealed class CreateModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IClassCreationService _classCreationService;

    public CreateModel(
        AppDbContext context,
        IClassCreationService classCreationService)
    {
        _context = context;
        _classCreationService = classCreationService;
    }

    [BindProperty]
    public CreateClassRequest Input { get; set; } = new();

    public ClassCreateOptionsResponse Options { get; private set; } =
        new(string.Empty, [], [], [], []);

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var context = await GetOwnerContextAsync(cancellationToken);

        if (context == null)
        {
            return RedirectToPage("/Login");
        }

        Input.CenterId = context.Value.CenterId;
        Input.StartDate = GetVietnamToday();
        Input.Schedules =
        [
            new ClassScheduleRequest { DayOfWeek = 1 }
        ];

        return await LoadOptionsAndPageAsync(context.Value, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var context = await GetOwnerContextAsync(cancellationToken);

        if (context == null)
        {
            return RedirectToPage("/Login");
        }

        Input.CenterId = context.Value.CenterId;
        var result = await _classCreationService.CreateAsync(
            context.Value.OwnerUserId,
            Input,
            cancellationToken);

        if (result.IsSuccess)
        {
            TempData["ToastMessage"] = result.Message;
            return RedirectToPage("/AdminClasses");
        }

        foreach (var error in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(error.Key)
                ? string.Empty
                : $"Input.{error.Key}";

            foreach (var message in error.Value)
            {
                ModelState.AddModelError(key, message);
            }
        }

        ModelState.AddModelError(string.Empty, result.Message);
        return await LoadOptionsAndPageAsync(context.Value, cancellationToken);
    }

    private async Task<IActionResult> LoadOptionsAndPageAsync(
        OwnerContext ownerContext,
        CancellationToken cancellationToken)
    {
        var result = await _classCreationService.GetCreateOptionsAsync(
            ownerContext.OwnerUserId,
            ownerContext.CenterId,
            cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }

        Options = result.Value;
        return Page();
    }

    private async Task<OwnerContext?> GetOwnerContextAsync(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var ownerUserId))
        {
            return null;
        }

        var centerId = await _context.Centers
            .AsNoTracking()
            .Where(center =>
                center.Status == "Active" &&
                (center.OwnerUserId == ownerUserId ||
                 _context.CenterUsers.Any(centerUser =>
                     centerUser.CenterId == center.CenterId &&
                     centerUser.UserId == ownerUserId &&
                     centerUser.UserType == "OWNER" &&
                     centerUser.Status == "Active")))
            .OrderBy(center => center.CenterId)
            .Select(center => (int?)center.CenterId)
            .FirstOrDefaultAsync(cancellationToken);

        return centerId.HasValue
            ? new OwnerContext(ownerUserId, centerId.Value)
            : null;
    }

    private static DateOnly GetVietnamToday()
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        }
        catch (TimeZoneNotFoundException)
        {
            return DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        }
    }

    private readonly record struct OwnerContext(int OwnerUserId, int CenterId);
}
