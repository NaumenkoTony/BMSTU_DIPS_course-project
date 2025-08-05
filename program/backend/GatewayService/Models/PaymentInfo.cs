namespace GatewayService.Models;

public partial class PaymentInfo
{
    public required string Status { get; set; }

    public int Price { get; set; }
}
