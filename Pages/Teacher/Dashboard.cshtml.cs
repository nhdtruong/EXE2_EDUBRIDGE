using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages.Teacher
{
    public class DashboardModel : PageModel
    {
        public DateTime CurrentDate { get; set; }

        public void OnGet()
        {
            CurrentDate = new DateTime(2026, 3, 24); // Hardcoded to match image for demo
        }
    }
}
