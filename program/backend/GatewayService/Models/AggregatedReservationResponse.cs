namespace GatewayService.Models;

public partial class AggregatedReservationResponse
{
    public required string ReservationUid { get; set; }

    public required HotelInfo? Hotel { get; set; }

    public required string StartDate { get; set; }

    public required string EndDate { get; set; }

    public required string Status { get; set; }

    public required PaymentInfo? Payment { get; set; }
}