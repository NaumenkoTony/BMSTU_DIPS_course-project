using AutoMapper;
using Contracts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Data;

namespace ReservationService.Controllers;

[Authorize]
public class HotelsController(IHotelRepository repository, IMapper mapper) : Controller
{
    private readonly IHotelRepository repository = repository;
    private readonly IMapper mapper = mapper;

    [Route("api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HotelResponse>>> GetAsync([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        var (hotels, totalCount) = await repository.GetHotelsAsync(page - 1, size);
    
    if (hotels == null || !hotels.Any())
    {
        return NoContent();
    }

    var response = new PaginatedResponse<HotelResponse>
    {
        Items = mapper.Map<IEnumerable<HotelResponse>>(hotels),
        TotalCount = totalCount,
        PageNumber = page,
        PageSize = size
    };

    return Ok(response);
}

    [Route("api/v1/[controller]/{uid}")]
    [HttpGet]
    public async Task<ActionResult<HotelResponse>> GetAsync(string uid)
    {
        return Ok(mapper.Map<HotelResponse>(await repository.GetByUidAsync(uid)));
    }
}