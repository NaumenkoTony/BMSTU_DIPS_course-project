namespace LoyaltyService.Controllers;

using AutoMapper;
using Contracts.Dto;
using LoyaltyService.Data;
using LoyaltyService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[Authorize]
public class LoyaltiesController : Controller
{
    private readonly ILoyalityRepository _repository;
    private readonly IMapper _mapper;
    private readonly ITokenService _tokenService;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<LoyaltiesController> _logger;

    public LoyaltiesController(ILoyalityRepository repository, IMapper mapper, 
        ITokenService tokenService, IKafkaProducer kafkaProducer, ILogger<LoyaltiesController> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _tokenService = tokenService;
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
                Service: "Loyalties",
                Action: action,
                Status: status,
                Timestamp: DateTime.UtcNow,
                Metadata: metadata ?? new Dictionary<string, object>()
            ),
            CancellationToken.None
        );
    }

    [Route("/api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<LoyaltyResponse>> GetByUsername()
    {
        var username = _tokenService.GetUsernameFromJWT();
        _logger.LogInformation("Get loyalty request from user: {Username}", username);

        try
        {
            var loyalty = await _repository.GetLoyalityByUsername(username);

            await PublishUserActionAsync(
                action: "LoyaltyStatusViewed",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["LoyaltyStatus"] = loyalty?.Status ?? "NotFound",
                    ["Discount"] = loyalty?.Discount ?? 0,
                    ["ReservationCount"] = loyalty?.ReservationCount ?? 0
                }
            );

            _logger.LogInformation("Loyalty found for user: {Username}, Status: {Status}, Discount: {Discount}",
                username, loyalty?.Status, loyalty?.Discount);
            
            return Ok(_mapper.Map<LoyaltyResponse>(loyalty));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loyalty for user: {Username}", username);
            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]/improve")]
    [HttpGet]
    public async Task<ActionResult> ImproveLoyality()
    {
        var username = _tokenService.GetUsernameFromJWT();
        _logger.LogInformation("Improve loyalty request from user: {Username}", username);

        try
        {
            var oldLoyalty = await _repository.GetLoyalityByUsername(username);
            await _repository.ImproveLoyality(username);
            var newLoyalty = await _repository.GetLoyalityByUsername(username);

            await PublishUserActionAsync(
                action: "LoyaltyImproved",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["OldStatus"] = oldLoyalty?.Status ?? "None",
                    ["NewStatus"] = newLoyalty?.Status ?? "None",
                    ["OldDiscount"] = oldLoyalty?.Discount ?? 0,
                    ["NewDiscount"] = newLoyalty?.Discount ?? 0,
                    ["ReservationCount"] = newLoyalty?.ReservationCount ?? 0
                }
            );

            _logger.LogInformation("Loyalty improved for user: {Username}", username);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error improving loyalty for user: {Username}", username);
            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]/degrade")]
    [HttpGet]
    public async Task<ActionResult> DegradeLoyality()
    {
        var username = _tokenService.GetUsernameFromJWT();
        var oldLoyalty = await _repository.GetLoyalityByUsername(username);
        _logger.LogInformation("Degrade loyalty request from user: {Username}", username);
        var newLoyalty = await _repository.GetLoyalityByUsername(username);

        await PublishUserActionAsync(
                        action: "LoyaltyDegraded",
                        status: "Success",
                        metadata: new Dictionary<string, object>
                        {
                            ["OldStatus"] = oldLoyalty?.Status ?? "None",
                            ["NewStatus"] = newLoyalty?.Status ?? "None",
                            ["OldDiscount"] = oldLoyalty?.Discount ?? 0,
                            ["NewDiscount"] = newLoyalty?.Discount ?? 0,
                            ["Reason"] = "ManualDegradation",
                            ["ReservationCount"] = newLoyalty?.ReservationCount ?? 0
                        }
                    );

        try
        {
            await _repository.DegradeLoyality(username);
            _logger.LogInformation("Loyalty degraded for user: {Username}", username);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error degrading loyalty for user: {Username}", username);
            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]/create-user")]
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult> CreateLoyalityUser([FromBody] string username)
    {
        _logger.LogInformation("Create loyalty user request for username: {Username}", username);

        try
        {
            await _repository.CreateLoyalityUser(username);
            _logger.LogInformation("Loyalty user created successfully: {Username}", username);

            await PublishUserActionAsync(
                action: "LoyaltyUserCreated",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["TargetUsername"] = username,
                    ["CreatedByAdmin"] = GetUsername(),
                    ["InitialStatus"] = "Bronze",
                    ["InitialDiscount"] = 5,
                }
            );

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loyalty user: {Username}", username);
            return StatusCode(500, "Internal server error");
        }
    }
}