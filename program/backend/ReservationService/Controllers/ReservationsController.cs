using AutoMapper;
using Contracts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Data;
using ReservationService.Models.DomainModels;
using ReservationService.Services;

namespace ReservationService.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private readonly IReservationRepository _repository;
    private readonly IHotelAvailabilityRepository _availabilityRepository;
    private readonly IHotelRepository _hotelRepository;
    private readonly IMapper _mapper;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<ReservationsController> _logger;
    private readonly ITokenService _tokenService;

    public ReservationsController(IReservationRepository repository, IHotelRepository hotelRepository, IHotelAvailabilityRepository availabilityRepository,
        IMapper mapper, ITokenService tokenService, IKafkaProducer kafkaProducer, ILogger<ReservationsController> logger)
    {
        _repository = repository;
        _hotelRepository = hotelRepository;
        _availabilityRepository = availabilityRepository;
        _mapper = mapper;
        _tokenService = tokenService;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    private string GetUserId()
    {
        return User.FindFirst("user_id")?.Value ?? "unknown";
    }

    private string GetUsername()
    {
        return User.Identity?.Name ?? "unknown";
    }

    private Task PublishUserActionAsync(string action, string status, Dictionary<string, object> metadata = null)
    {
        _ = Task.Run(async () =>
        {
            var userId = GetUserId();
            var username = GetUsername();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            try
            {
                await _kafkaProducer.PublishAsync(
                    topic: "user-actions",
                    key: userId,
                    message: new UserAction(
                        UserId: userId,
                        Username: username,
                        Service: "Reservation",
                        Action: action,
                        Status: status,
                        Timestamp: DateTimeOffset.UtcNow,
                        Metadata: metadata ?? new Dictionary<string, object>()
                    ),
                    cts.Token
                );
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Kafka timeout for {Action}", action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kafka failed for {Action}", action);
            }
        });

        return Task.CompletedTask;
    }

    [Route("/api/v1/[controller]")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationResponse>>> GetByUsernameAsync()
    {
        var username = _tokenService.GetUsernameFromJWT();
        _logger.LogInformation("Get reservations request from user: {Username}", username);

        try
        {
            var reservations = await _repository.GetReservationsByUsernameAsync(username);

            _logger.LogInformation("Found {Count} reservations for user: {Username}",
                reservations.Count(), username);
            await PublishUserActionAsync(
               action: "GetReservationsList",
               status: "Success",
               metadata: new Dictionary<string, object>
               {
                   ["ReservationsCount"] = reservations.Count(),
                   ["ActiveReservations"] = reservations.Count(r => r.Status == "PAID"),
                   ["CancelledReservations"] = reservations.Count(r => r.Status == "CANCELLED"),
                   ["FirstReservationUid"] = reservations.FirstOrDefault()?.ReservationUid,
                   ["LastReservationUid"] = reservations.LastOrDefault()?.ReservationUid
               }
           );

            return Ok(_mapper.Map<IEnumerable<ReservationResponse>>(reservations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservations for user: {Username}", username);
            await PublishUserActionAsync(
                action: "GetReservationsList",
                status: "Failed",
                metadata: new Dictionary<string, object>
                {
                    ["Error"] = ex.Message
                }
            );

            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]/")]
    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> CreateReservationAsync([FromBody] ReservationRequest reservationRequest)
    {
        var username = _tokenService.GetUsernameFromJWT();
        _logger.LogInformation("Create reservation request from user: {Username}. HotelId: {HotelId}, Status: {Status}",
            username, reservationRequest.HotelId, reservationRequest.Status);

        try
        {
            var reservation = _mapper.Map<Reservation>(reservationRequest);
            await _repository.CreateAsync(reservation);

            _logger.LogInformation("Reservation created successfully. UID: {ReservationUid}, ID: {ReservationId}",
                reservation.ReservationUid, reservation.Id);
            await PublishUserActionAsync(
            action: "CreateReservation",
            status: "Success",
            metadata: new Dictionary<string, object>
            {
                ["ReservationUid"] = reservation.ReservationUid,
                ["HotelUid"] = reservation.Hotel?.HotelUid,
                ["HotelName"] = reservation.Hotel?.Name,
                ["Country"] = reservation.Hotel?.Country,
                ["City"] = reservation.Hotel?.City,
                ["StartDate"] = reservation.StartDate,
                ["EndDate"] = reservation.EndDate,
                ["Status"] = reservation.Status
            });

            var startDate = DateTime.Parse(reservationRequest.StartDate).ToUniversalTime();
            var endDate = DateTime.Parse(reservationRequest.EndDate).ToUniversalTime();
            foreach (var date in EachDate(startDate, endDate))
            {
                var availability = await _availabilityRepository.GetByDateAsync(reservation.HotelId, date);
                if (availability == null)
                    return BadRequest($"No availability data for {date:yyyy-MM-dd}");

                if (availability.AvailableRooms <= 0)
                    return BadRequest($"No rooms available on {date:yyyy-MM-dd}");

                availability.AvailableRooms -= 1;
                await _availabilityRepository.UpdateAsync(availability);
            }

            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation for user: {Username}. HotelId: {HotelId}",
                username, reservationRequest.HotelId);
            await PublishUserActionAsync(
            action: "CreateReservation",
            status: "Failed",
            metadata: new Dictionary<string, object>
            {
                ["HotelId"] = reservationRequest.HotelId,
                ["StartDate"] = reservationRequest.StartDate,
                ["EndDate"] = reservationRequest.EndDate,
                ["Error"] = ex.Message
            });

            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]")]
    [HttpPut]
    public async Task<ActionResult<ReservationResponse>> UpdateReservationStatusAsync([FromBody] ReservationResponse reservationResponse)
    {
        var username = _tokenService.GetUsernameFromJWT();
        _logger.LogInformation("Update reservation request from user: {Username}. UID: {ReservationUid}, New Status: {Status}",
            username, reservationResponse.ReservationUid, reservationResponse.Status);

        try
        {
            var reservation = await _repository.GetByUidAsync(reservationResponse.ReservationUid);

            if (reservation == null)
            {
                _logger.LogWarning("Reservation not found for update. UID: {ReservationUid}", reservationResponse.ReservationUid);
                await PublishUserActionAsync(
                    action: "UpdateReservation",
                    status: "NotFound",
                    metadata: new Dictionary<string, object>
                    {
                        ["ReservationUid"] = reservationResponse.ReservationUid,
                        ["Error"] = "Reservation not found"
                    }
                );

                return NotFound();
            }

            var oldStatus = reservation.Status;
            var newModel = _mapper.Map<Reservation>(reservationResponse);
            newModel.Id = reservation.Id;

            await _repository.UpdateAsync(newModel, reservation.Id);

            _logger.LogInformation("Reservation updated successfully. UID: {ReservationUid}, Old Status: {OldStatus}, New Status: {NewStatus}",
                reservationResponse.ReservationUid, reservation.Status, reservationResponse.Status);
            await PublishUserActionAsync(
                action: "UpdateReservation",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["ReservationUid"] = reservationResponse.ReservationUid,
                    ["OldStatus"] = oldStatus,
                    ["NewStatus"] = reservationResponse.Status,
                    ["StartDate"] = reservation.StartDate,
                    ["EndDate"] = reservation.EndDate,
                    ["UpdatedDate"] = DateTime.UtcNow,
                }
            );

            var startDate = DateTime.Parse(reservationResponse.StartDate).ToUniversalTime();
            var endDate = DateTime.Parse(reservationResponse.EndDate).ToUniversalTime();
            if (oldStatus == "PAID" && reservationResponse.Status == "CANCELLED")
            {
                foreach (var date in EachDate(startDate, endDate))
                {
                    var availability = await _availabilityRepository.GetByDateAsync(reservation.HotelId, date);
                    if (availability != null)
                    {
                        availability.AvailableRooms += 1;
                        await _availabilityRepository.UpdateAsync(availability);
                    }
                }
            }

            return Ok(newModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation for user: {Username}. UID: {ReservationUid}",
                username, reservationResponse.ReservationUid);
            await PublishUserActionAsync(
                action: "UpdateReservation",
                status: "Failed",
                metadata: new Dictionary<string, object>
                {
                    ["ReservationUid"] = reservationResponse.ReservationUid,
                    ["NewStatus"] = reservationResponse.Status,
                    ["Error"] = ex.Message
                }
            );

            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]/hotels/{id}")]
    [HttpGet]
    public async Task<ActionResult<HotelResponse>> GetHotelAsync(int id)
    {
        _logger.LogInformation("Get hotel request. Hotel ID: {HotelId}", id);

        try
        {
            var hotel = await _hotelRepository.ReadAsync(id);
            if (hotel == null)
            {
                _logger.LogWarning("Hotel not found. ID: {HotelId}", id);
                await PublishUserActionAsync(
                    action: "GetHotel",
                    status: "NotFound",
                    metadata: new Dictionary<string, object>
                    {
                        ["HotelId"] = id,
                        ["Error"] = "Hotel not found"
                    }
                );

                return NotFound();
            }

            _logger.LogInformation("Hotel found: ID={HotelId}, Name={HotelName}", id, hotel.Name);
            await PublishUserActionAsync(
                action: "GetHotel",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["HotelId"] = hotel.Id,
                    ["HotelUid"] = hotel.HotelUid,
                    ["HotelName"] = hotel.Name,
                    ["Country"] = hotel.Country,
                    ["City"] = hotel.City,
                    ["Address"] = hotel.Address,
                    ["Stars"] = hotel.Stars,
                    ["Price"] = hotel.Price,
                }
            );

            return Ok(_mapper.Map<HotelResponse>(hotel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel. ID: {HotelId}", id);

            await PublishUserActionAsync(
               action: "GetHotel",
               status: "Failed",
               metadata: new Dictionary<string, object>
               {
                   ["HotelId"] = id,
                   ["Error"] = ex.Message
               }
           );

            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]/{uid}")]
    [HttpGet]
    public async Task<ActionResult<ReservationResponse>> GetReservationAsync(string uid)
    {
        var username = _tokenService.GetUsernameFromJWT();
        _logger.LogInformation("Get reservation request from user: {Username}. UID: {ReservationUid}",
            username, uid);

        try
        {
            var reservation = await _repository.GetByUsernameUidAsync(username, uid);

            if (reservation == null)
            {
                _logger.LogWarning("Reservation not found for user: {Username}. UID: {ReservationUid}",
                    username, uid);
                await PublishUserActionAsync(
                    action: "GetReservation",
                    status: "NotFound",
                    metadata: new Dictionary<string, object>
                    {
                        ["ReservationUid"] = uid,
                        ["Error"] = "Reservation not found"
                    }
                );

                return NotFound();
            }

            _logger.LogInformation("Reservation found: UID={ReservationUid}, Status={Status}",
                uid, reservation.Status);
            await PublishUserActionAsync(
                action: "GetReservation",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["ReservationUid"] = reservation.ReservationUid,
                    ["Status"] = reservation.Status,
                    ["HotelUid"] = reservation.Hotel?.HotelUid,
                    ["StartDate"] = reservation.StartDate,
                    ["EndDate"] = reservation.EndDate,
                }
            );

            return Ok(_mapper.Map<ReservationResponse>(reservation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation for user: {Username}. UID: {ReservationUid}",
                username, uid);
            await PublishUserActionAsync(
                action: "ReservationViewed",
                status: "Failed",
                metadata: new Dictionary<string, object>
                {
                    ["ReservationUid"] = uid,
                    ["Error"] = ex.Message
                }
            );

            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/hotels/{hotelId}/availability")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAvailabilityAsync(
        int hotelId,
        DateTime? from,
        DateTime? to)
    {
        var start = from ?? DateTime.UtcNow.Date;
        var end = to ?? start.AddMonths(3);

        if (end < start)
            return BadRequest("Invalid date range");

        var availability = await _availabilityRepository.GetAvailabilityAsync(hotelId, start, end);

        var result = availability.Select(a => new
        {
            a.Date,
            a.AvailableRooms
        });

        return Ok(result);
    }
    
    private IEnumerable<DateTime> EachDate(DateTime from, DateTime to)
    {
        for (var day = from.Date; day < to.Date; day = day.AddDays(1))
            yield return day;
    }
}