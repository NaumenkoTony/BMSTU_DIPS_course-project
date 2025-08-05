namespace ReservationService.Data;

using Contracts;
using ReservationService.Models.DomainModels;

public interface IHotelRepository : IRepository<Hotel>
{
    public Task<IEnumerable<Hotel>> GetHotelsAsync(int page, int size);

    public Task<Hotel?> GetByUidAsync(string uid);
}
