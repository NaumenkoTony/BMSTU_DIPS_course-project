namespace ReservationService.Data.RepositoriesPostgreSQL;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ReservationService.Models.DomainModels;

public class HotelAvailabilityRepository(ReservationsContext context) : Repository<HotelAvailability>(context), IHotelAvailabilityRepository
{
    private readonly ReservationsContext _context = context;
    
    public async Task<IEnumerable<HotelAvailability>> GetAvailabilityAsync(string hotelUid, DateTime from, DateTime to)
    {
        if (!Guid.TryParse(hotelUid, out var parsedUid))
            return Enumerable.Empty<HotelAvailability>();

        var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);

        return await _context.HotelAvailabilities
            .Where(a => a.Hotel.HotelUid == parsedUid 
                        && a.Date >= fromUtc 
                        && a.Date <= toUtc)
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