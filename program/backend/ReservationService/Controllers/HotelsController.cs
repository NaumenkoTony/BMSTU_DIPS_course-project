using AutoMapper;
using Contracts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Data;
using Microsoft.Extensions.Logging;
using ReservationService.Services;

namespace ReservationService.Controllers;

[Authorize]
public class HotelsController : Controller
{
    private readonly IHotelRepository _repository;
    private readonly IMapper _mapper;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<HotelsController> _logger;

    public HotelsController(IHotelRepository repository, IMapper mapper, IKafkaProducer kafkaProducer, ILogger<HotelsController> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst("sub")?.Value ?? "unknown";
    }

    private string GetUsername()
    {
        return User.Identity?.Name ?? "unknown";
    }

    private async Task PublishUserActionAsync(string action, string status, Dictionary<string, object> metadata = null)
    {
        var userId = GetUserId();
        var username = GetUsername();

        await _kafkaProducer.PublishAsync(
            topic: "user-actions",
            key: userId,
            message: new UserAction(
                UserId: userId,
                Username: username,
                Service: "Hotels",
                Action: action,
                Status: status,
                Timestamp: DateTime.UtcNow,
                Metadata: metadata ?? new Dictionary<string, object>()
            ),
            CancellationToken.None
        );
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

                await PublishUserActionAsync(
                    action: "HotelsListViewed",
                    status: "NoContent",
                    metadata: new Dictionary<string, object>
                    {
                        ["Page"] = page,
                        ["PageSize"] = size,
                        ["TotalCount"] = 0,
                        ["HotelsCount"] = 0
                    }
                );

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

            await PublishUserActionAsync(
                action: "HotelsListViewed",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["Page"] = page,
                    ["PageSize"] = size,
                    ["TotalCount"] = totalCount,
                    ["HotelsCount"] = hotels.Count(),
                    ["FirstHotelUid"] = hotels.FirstOrDefault()?.HotelUid,
                    ["LastHotelUid"] = hotels.LastOrDefault()?.HotelUid,
                }
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotels. Page: {Page}, Size: {Size}", page, size);

            await PublishUserActionAsync(
                action: "HotelsListViewed",
                status: "Failed",
                metadata: new Dictionary<string, object>
                {
                    ["Page"] = page,
                    ["PageSize"] = size,
                    ["Error"] = ex.Message
                }
            );

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
                await PublishUserActionAsync(
                    action: "HotelViewed",
                    status: "NotFound",
                    metadata: new Dictionary<string, object>
                    {
                        ["HotelUid"] = uid,
                        ["Error"] = "Hotel not found"
                    }
                );

                return NotFound();
            }

            _logger.LogInformation("Hotel found: UID={HotelUid}, Name={HotelName}", uid, hotel.Name);
            await PublishUserActionAsync(
                action: "HotelViewed",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["HotelUid"] = hotel.HotelUid,
                    ["HotelName"] = hotel.Name,
                    ["Country"] = hotel.Country,
                    ["City"] = hotel.City,
                    ["Address"] = hotel.Address,
                    ["Stars"] = hotel.Stars,
                    ["Price"] = hotel.Price,
                    ["Currency"] = hotel.Price,
                }
            );
            
            return Ok(_mapper.Map<HotelResponse>(hotel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel for UID: {HotelUid}", uid);
             await PublishUserActionAsync(
                action: "HotelViewed",
                status: "Failed",
                metadata: new Dictionary<string, object>
                {
                    ["HotelUid"] = uid,
                    ["Error"] = ex.Message
                }
            );
            return StatusCode(500, "Internal server error");
        }
    }
}