namespace LoyaltyService.Data.RepositoriesPostgreSQL;

using LoyaltyService.Models.DomainModels;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class LoyalityRepository(LoyaltiesContext context) : Repository<Loyalty>(context), ILoyalityRepository
{
    private LoyaltiesContext db = context;

    public async Task<Loyalty?> GetLoyalityByUsername(string username)
    {
        var loyalty = await db.Loyalties.FirstOrDefaultAsync(r => r.Username == username);
        if (loyalty == null)
        {
            await CreateLoyalityUser(username);
            loyalty = await db.Loyalties.FirstOrDefaultAsync(r => r.Username == username);
        }
        return loyalty;        
    }

    public async Task ImproveLoyality(string username)
    {
        var loyalty = await db.Loyalties.FirstOrDefaultAsync(r => r.Username == username);

        if (loyalty == null)
        {
            await CreateLoyalityUser(username);
            loyalty = await db.Loyalties.FirstOrDefaultAsync(r => r.Username == username);
        }
        if (loyalty != null)
        {
            loyalty.ReservationCount++;
            if (loyalty.ReservationCount >= 20)
            {
                loyalty.Status = "GOLD";
                loyalty.Discount = 10;
            }
            else if (loyalty.ReservationCount >= 10)
            {
                loyalty.Status = "SILVER";
                loyalty.Discount = 7;
            }
            else
            {
                loyalty.Status = "BRONZE";
                loyalty.Discount = 5;
            }

            await db.SaveChangesAsync();
        }
        else
        {
            throw new Exception("User not found");
        }
    }

    public async Task DegradeLoyality(string username)
    {
        var loyalty = await db.Loyalties.FirstOrDefaultAsync(r => r.Username == username);

        if (loyalty == null)
        {
            
            await CreateLoyalityUser(username);
            loyalty = await db.Loyalties.FirstOrDefaultAsync(r => r.Username == username);
        }
        if (loyalty != null)
        {
            loyalty.ReservationCount--;
            if (loyalty.ReservationCount >= 20)
            {
                loyalty.Status = "GOLD";
                loyalty.Discount = 10;
            }
            else if (loyalty.ReservationCount >= 10)
            {
                loyalty.Status = "SILVER";
                loyalty.Discount = 7;
            }
            else
            {
                loyalty.Status = "BRONZE";
                loyalty.Discount = 5;
            }

            await db.SaveChangesAsync();
        }
        else
        {
            throw new Exception("User not found");
        }
    }

    public async Task CreateLoyalityUser(string username)
    {
        await db.Loyalties.AddAsync(new()
        {
            Username = username,
            Discount = 5
        });
        await db.SaveChangesAsync();
    }
}