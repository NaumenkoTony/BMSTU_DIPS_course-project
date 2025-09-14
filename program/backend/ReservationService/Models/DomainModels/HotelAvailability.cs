namespace ReservationService.Models.DomainModels;

public class HotelAvailability
{
    public int Id { get; set; }
    public int HotelId { get; set; }
    public DateTime Date { get; set; }
    public int AvailableRooms { get; set; }

    public Hotel Hotel { get; set; } = null!;
}
