using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StatisticsService.Data;

namespace StatisticsService.Controllers
{
    [ApiController]
    [Route("api/v1/reports")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly StatisticsDbContext _db;

        public ReportsController(StatisticsDbContext db)
        {
            _db = db;
        }

        [HttpGet("user-actions")]
        public IActionResult GetUserActions([FromQuery] string? userId)
        {
            var query = _db.UserActions.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(x => x.UserId == userId);

            return Ok(query.OrderByDescending(x => x.Timestamp).Take(100).ToList());
        }
    }
}
