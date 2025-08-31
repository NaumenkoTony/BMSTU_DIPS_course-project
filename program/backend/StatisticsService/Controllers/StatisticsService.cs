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
    public async Task<ActionResult<StatisticsSummary>> GetSummary()
    {
        var total = await _db.UserActions.CountAsync();
        var unique = await _db.UserActions.Select(u => u.UserId).Distinct().CountAsync();
        var byType = await _db.UserActions
            .GroupBy(u => u.Action)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
        var byDay = await _db.UserActions
            .GroupBy(u => u.Timestamp.Date)
            .ToDictionaryAsync(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());

        return new StatisticsSummary
        {
            TotalActions = total,
            UniqueUsers = unique,
            ActionsByType = byType,
            ActionsByDay = byDay
        };
    }
}
