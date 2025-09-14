using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models.DomainModels;

namespace ReservationService.Services;

public class AvailabilityService
{
    private readonly ReservationsContext _context;

    public AvailabilityService(ReservationsContext context)
    {
        _context = context;
    }

    public async Task EnsureAvailabilityWindowAsync(int yearsAhead = 3, int defaultRooms = 100)
    {
        var today = DateTime.UtcNow.Date;
        var endDate = today.AddYears(yearsAhead);

        var hotels = await _context.Hotels.ToListAsync();

        foreach (var hotel in hotels)
        {
            var existingDates = await _context.HotelAvailabilities
                .Where(a => a.HotelId == hotel.Id && a.Date >= today)
                .Select(a => a.Date)
                .ToListAsync();

            var datesToAdd = Enumerable.Range(0, (endDate - today).Days + 1)
                .Select(offset => today.AddDays(offset))
                .Except(existingDates);

            foreach (var date in datesToAdd)
            {
                _context.HotelAvailabilities.Add(new HotelAvailability
                {
                    HotelId = hotel.Id,
                    Date = date,
                    AvailableRooms = hotel.RoomsCount
                });
            }
        }

        var old = _context.HotelAvailabilities.Where(a => a.Date < today);
        _context.HotelAvailabilities.RemoveRange(old);

        await _context.SaveChangesAsync();
    }
}
