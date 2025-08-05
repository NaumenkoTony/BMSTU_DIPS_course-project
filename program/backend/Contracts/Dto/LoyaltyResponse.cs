namespace Contracts.Dto;

public class LoyaltyResponse
{
    public required string Status { get; set; }
    public int Discount { get; set; }
    public int ReservationCount { get; set; }
}
