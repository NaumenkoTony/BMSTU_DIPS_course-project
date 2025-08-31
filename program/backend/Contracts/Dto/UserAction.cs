namespace Contracts.Dto;

public class UserAction(string UserId, string Username, string Service, string Action, string Status, DateTime Timestamp, Dictionary<string, object> Metadata)
{
    public string UserId { get; set; } = UserId;
    public string Username { get; set; } = Username;
    public string Service { get; set; } = Service;
    public string Action { get; set; } = Action;
    public string Status { get; set; } = Status;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}
