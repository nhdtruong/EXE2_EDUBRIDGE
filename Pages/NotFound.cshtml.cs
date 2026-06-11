using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages
{
    [AllowAnonymous]
    public class NotFoundModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
