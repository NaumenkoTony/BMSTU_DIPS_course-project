namespace StatisticsService.Models;

public class UserActionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Service { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }

    public string? MetadataJson { get; set; }

    public string Topic { get; set; } = null!;
    public int Partition { get; set; }
    public long Offset { get; set; }
}