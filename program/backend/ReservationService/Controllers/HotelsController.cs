using AutoMapper;
using Contracts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Data;
using Microsoft.Extensions.Logging;

namespace ReservationService.Controllers;

[Authorize]
public class HotelsController : Controller
{
    private readonly IHotelRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<HotelsController> _logger;

    public HotelsController(IHotelRepository repository, IMapper mapper, ILogger<HotelsController> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    [Route("api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HotelResponse>>> GetAsync([FromQuery] int page = 1, [FromQuery] int size = 10)
    {
        _logger.LogInformation("Get hotels request. Page: {Page}, Size: {Size}", page, size);

        try
        {
            var (hotels, totalCount) = await _repository.GetHotelsAsync(page - 1, size);
        
            if (hotels == null || !hotels.Any())
            {
                _logger.LogInformation("No hotels found for page: {Page}, size: {Size}", page, size);
                return NoContent();
            }

            _logger.LogDebug("Retrieved {Count} hotels out of {TotalCount}", hotels.Count(), totalCount);

            var response = new PaginatedResponse<HotelResponse>
            {
                Items = _mapper.Map<IEnumerable<HotelResponse>>(hotels),
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = size
            };

            _logger.LogInformation("Hotels retrieved successfully. Page: {Page}, Size: {Size}, Total: {TotalCount}", 
                page, size, totalCount);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotels. Page: {Page}, Size: {Size}", page, size);
            return StatusCode(500, "Internal server error");
        }
    }

    [Route("api/v1/[controller]/{uid}")]
    [HttpGet]
    public async Task<ActionResult<HotelResponse>> GetAsync(string uid)
    {
        _logger.LogInformation("Get hotel request for UID: {HotelUid}", uid);

        try
        {
            var hotel = await _repository.GetByUidAsync(uid);
            
            if (hotel == null)
            {
                _logger.LogWarning("Hotel not found for UID: {HotelUid}", uid);
                return NotFound();
            }

            _logger.LogInformation("Hotel found: UID={HotelUid}, Name={HotelName}", uid, hotel.Name);
            
            return Ok(_mapper.Map<HotelResponse>(hotel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel for UID: {HotelUid}", uid);
            return StatusCode(500, "Internal server error");
        }
    }
}