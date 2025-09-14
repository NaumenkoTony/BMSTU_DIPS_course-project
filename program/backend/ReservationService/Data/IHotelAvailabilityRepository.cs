namespace ReservationService.Data;

using Contracts;
using ReservationService.Models.DomainModels;

public interface IHotelAvailabilityRepository : IRepository<HotelAvailability>
{
    Task<IEnumerable<HotelAvailability>> GetAvailabilityAsync(string hotelUid, DateTime from, DateTime to);
    Task<HotelAvailability?> GetByDateAsync(int? hotelId, DateTime date);
    Task UpdateAsync(HotelAvailability availability);
    Task AddAsync(HotelAvailability availability);
}