namespace ReservationService.Data.RepositoriesPostgreSQL;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ReservationService.Models.DomainModels;

public class HotelAvailabilityRepository(ReservationsContext context) : Repository<HotelAvailability>(context), IHotelAvailabilityRepository
{
    private readonly ReservationsContext _context = context;
    
    public async Task<IEnumerable<HotelAvailability>> GetAvailabilityAsync(int hotelId, DateTime from, DateTime to)
    {
        return await _context.HotelAvailabilities
            .Where(a => a.HotelId == hotelId && a.Date >= from && a.Date <= to)
            .OrderBy(a => a.Date)
            .ToListAsync();
    }

    public async Task<HotelAvailability?> GetByDateAsync(int? hotelId, DateTime date)
    {
        return await _context.HotelAvailabilities
            .FirstOrDefaultAsync(a => a.HotelId == hotelId && a.Date == date.Date);
    }

    public async Task UpdateAsync(HotelAvailability availability)
    {
        _context.HotelAvailabilities.Update(availability);
        await _context.SaveChangesAsync();
    }

    public async Task AddAsync(HotelAvailability availability)
    {
        _context.HotelAvailabilities.Add(availability);
        await _context.SaveChangesAsync();
    }
}