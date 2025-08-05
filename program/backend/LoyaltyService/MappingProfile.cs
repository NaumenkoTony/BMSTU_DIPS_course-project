namespace LoyaltyService;

using AutoMapper;
using Contracts.Dto;
using LoyaltyService.Models.DomainModels;
public class MappingProfile : Profile
{
    public MappingProfile()
     {
        CreateMap<Loyalty, LoyaltyResponse>().ReverseMap();
    }
}
