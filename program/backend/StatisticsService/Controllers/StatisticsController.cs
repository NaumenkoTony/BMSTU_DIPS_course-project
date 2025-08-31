using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StatisticsService.Data;
using StatisticsService.Models;

namespace StatisticsService.Controllers;

[ApiController]
[Route("api/v1/statistics")]
[Authorize(Roles = "Admin")]
public class StatisticsController : ControllerBase
{
    private readonly StatisticsDbContext _db;

    public StatisticsController(StatisticsDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary()
    {
        var total = await _db.UserActions.CountAsync();

        var threshold = DateTime.UtcNow.AddMinutes(-15);
        var activeUsers = await _db.UserActions
            .Where(u => u.Timestamp >= threshold)
            .Select(u => u.Username)
            .Distinct()
            .CountAsync();

        var byType = await _db.UserActions
            .GroupBy(u => u.Action)
            .ToDictionaryAsync(g => g.Key, g => g.Count());

        var byDay = await _db.UserActions
            .GroupBy(u => u.Timestamp.Date)
            .ToDictionaryAsync(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

        var usersWithCounts = await _db.UserActions
            .GroupBy(u => u.Username)
            .Select(g => new { Username = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync();

        return Ok(new
        {
            TotalActions = total,
            ActiveUsers = activeUsers,
            ActionsByType = byType,
            ActionsByDay = byDay,
            TopUsers = usersWithCounts
        });
    }

    [HttpGet("recent")]
    public async Task<ActionResult<object>> GetRecentActions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? username = null)
    {
        var query = _db.UserActions.AsQueryable();

        if (!string.IsNullOrEmpty(username))
            query = query.Where(a => a.Username == username);

        var total = await query.CountAsync();

        var actions = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { Items = actions, TotalCount = total });
    }

}
