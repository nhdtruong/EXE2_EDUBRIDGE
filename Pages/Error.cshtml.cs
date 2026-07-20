using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduBridge.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet() => HandleError();
        public IActionResult OnPost() => HandleError();
        public IActionResult OnPut() => HandleError();
        public IActionResult OnDelete() => HandleError();
        public IActionResult OnPatch() => HandleError();

        private IActionResult HandleError()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature?.Error != null)
            {
                ExceptionMessage = exceptionHandlerPathFeature.Error.Message;
                StackTrace = exceptionHandlerPathFeature.Error.StackTrace;
                _logger.LogError(exceptionHandlerPathFeature.Error, "Unhandled exception captured in Error page");
            }

            if (exceptionHandlerPathFeature?.Path.StartsWith("/api") == true)
            {
                return new JsonResult(new { message = "Internal server error", traceId = RequestId, error = ExceptionMessage }) { StatusCode = 500 };
            }

            return Page();
        }
    }

}
