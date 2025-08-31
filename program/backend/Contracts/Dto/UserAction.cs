namespace Contracts.Dto;

public class UserAction
{
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Action { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
