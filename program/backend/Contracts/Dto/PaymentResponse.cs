namespace Contracts.Dto;

public class PaymentResponse
{
    public required string Id { get; set; }
    
    public required string PaymentUid { get; set; }

    public required string Status { get; set; }

    public int Price { get; set; }
}
