namespace ReservationService.Data;

using Contracts;
using ReservationService.Models.DomainModels;

public interface IReservationRepository : IRepository<Reservation>
{
    public Task<IEnumerable<Reservation>> GetReservationsByUsernameAsync(string username);

    public Task<Reservation?> GetByUidAsync(string uid);

    public Task<Reservation?> GetByUsernameUidAsync(string username, string uid);
}
