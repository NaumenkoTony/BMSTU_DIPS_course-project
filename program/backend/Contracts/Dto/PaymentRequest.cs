namespace Contracts.Dto;

public class PaymentRequest
{
    public required string Status { get; set; }

    public int Price { get; set; }
}
