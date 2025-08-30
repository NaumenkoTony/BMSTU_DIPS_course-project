namespace LoyaltyService.Data;

using Contracts;
using LoyaltyService.Models.DomainModels;

public interface ILoyalityRepository : IRepository<Loyalty>
{
    public Task<Loyalty?> GetLoyalityByUsername(string username);

    public Task ImproveLoyality(string username);
    public Task DegradeLoyality(string username);

    public Task CreateLoyalityUser(string username);
}
