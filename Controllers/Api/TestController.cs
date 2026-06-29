using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduBridge.Data;
using System.Threading.Tasks;

namespace EduBridge.Controllers.Api
{
    [ApiController]
    [Route("api/test-db")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var dbName = _context.Database.GetDbConnection().Database;
            var server = _context.Database.GetDbConnection().DataSource;
            
            bool hasAttachmentUrl = false;
            try {
                await _context.Database.ExecuteSqlRawAsync("SELECT TOP 1 AttachmentUrl FROM Homework");
                hasAttachmentUrl = true;
            } catch (System.Exception ex) {
                return Ok(new { server, dbName, error = ex.Message });
            }

            return Ok(new { server, dbName, hasAttachmentUrl });
        }
    }
}
