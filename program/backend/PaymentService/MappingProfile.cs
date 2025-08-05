namespace PaymentService;

using AutoMapper;
using Contracts.Dto;
using PaymentService.Models.DomainModels;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Payment, PaymentResponse>()
            .ForMember(dest => dest.PaymentUid, opt => opt.MapFrom(src => src.PaymentUid.ToString()));

        CreateMap<PaymentResponse, Payment>()
            .ForMember(dest => dest.PaymentUid, opt => opt.MapFrom(src => Guid.Parse(src.PaymentUid)));

        CreateMap<Payment, PaymentRequest>().ReverseMap();
    }
}
