using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToPage("/Login");
            }

            if (User.IsInRole("OWNER"))
            {
                return RedirectToPage("/AdminDashboard");
            }

            if (User.IsInRole("TEACHER"))
            {
                return RedirectToPage("/Teacher/Dashboard");
            }

            if (User.IsInRole("PARENT"))
            {
                return RedirectToPage("/Messages");
            }

            return RedirectToPage("/Login");
        }
    }
}
