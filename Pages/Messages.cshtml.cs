using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace EduBridge.Pages
{
    public class MessagesModel : PageModel
    {
        public int CurrentUserId { get; set; }

        public void OnGet()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out int userId))
            {
                CurrentUserId = userId;
            }
        }
    }
}
