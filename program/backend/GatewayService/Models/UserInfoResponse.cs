namespace GatewayService.Models;

using System.Text.Json.Serialization;
using Contracts.Dto;

public class UserInfoResponse
{
    public List<AggregatedReservationResponse> Reservations { get; set; } = [];

    [JsonIgnore]
    public LoyaltyResponse? Loyalty { get; set; }

    [JsonPropertyName("loyalty")]
    public object LoyaltyJson => Loyalty != null ? (object)Loyalty : "";
}