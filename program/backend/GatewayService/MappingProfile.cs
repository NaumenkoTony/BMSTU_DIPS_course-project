using AutoMapper;
using GatewayService.Models;
using Contracts.Dto;


public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<HotelResponse, HotelInfoResponse>();
        
        CreateMap<HotelResponse, HotelInfo>()
            .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => $"{src.Country}, {src.City}, {src.Address}"));

        CreateMap<LoyaltyResponse, LoyaltyInfoResponse>().ReverseMap();

        CreateMap<PaymentResponse, PaymentInfo>().ReverseMap();
    }
}