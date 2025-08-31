namespace LoyaltyService.Data.RepositoriesPostgreSQL;

using LoyaltyService.Models.DomainModels;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

public class LoyalityRepository(LoyaltiesContext context) : Repository<Loyalty>(context), ILoyalityRepository
{
    private LoyaltiesContext db = context;

    public async Task<Loyalty?> GetLoyalityByUsername(string username)
    {
        return await db.Loyalties.FirstOrDefaultAsync(r => r.Username == username);
    }

    public async Task<Loyalty> GetOrCreateLoyalityByUsername(string username)
    {
        var loyalty = await GetLoyalityByUsername(username);
        
        if (loyalty == null)
        {
            loyalty = await CreateLoyalityUser(username);
        }
        
        return loyalty;
    }

    public async Task ImproveLoyality(string username)
    {
        var loyalty = await GetOrCreateLoyalityByUsername(username);

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

    public async Task DegradeLoyality(string username)
    {
        var loyalty = await GetOrCreateLoyalityByUsername(username);

        loyalty.ReservationCount = Math.Max(0, loyalty.ReservationCount - 1);
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

    public async Task<Loyalty> CreateLoyalityUser(string username)
    {
        var existing = await GetLoyalityByUsername(username);
        if (existing != null)
        {
            return existing;
        }

        try
        {
            var loyalty = new Loyalty
            {
                Username = username,
                Status = "BRONZE",
                Discount = 5,
                ReservationCount = 0
            };
            
            db.Loyalties.Add(loyalty);
            await db.SaveChangesAsync();
            return loyalty;
        }
        catch (DbUpdateException ex) when (IsDuplicateKeyException(ex))
        {
            return await GetLoyalityByUsername(username) 
                ?? throw new Exception($"Failed to create and user {username} not found");
        }
    }

    private bool IsDuplicateKeyException(DbUpdateException ex)
    {
        return ex.InnerException is Npgsql.PostgresException pgEx && 
               pgEx.SqlState == "23505";
    }
}