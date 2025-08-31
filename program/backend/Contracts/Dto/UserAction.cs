namespace Contracts.Dto;

public class UserAction(string UserId, string Username, string Service, string Action, string Status, DateTimeOffset Timestamp, Dictionary<string, object> Metadata)
{
    public string UserId { get; set; } = UserId;
    public string Username { get; set; } = Username;
    public string Service { get; set; } = Service;
    public string Action { get; set; } = Action;
    public string Status { get; set; } = Status;
    public DateTimeOffset Timestamp { get; set; } = Timestamp;
    public Dictionary<string, object>? Metadata { get; set; } = Metadata;
}
