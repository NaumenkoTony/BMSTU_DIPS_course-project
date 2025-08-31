namespace StatisticsService.Models;

public class StatisticsSummary
{
    public int TotalActions { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> ActionsByType { get; set; } = new();
    public Dictionary<string, int> ActionsByDay { get; set; } = new();
}
