using AutoMapper;
using Contracts.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.Data;
using ReservationService.Models.DomainModels;
using ReservationService.TokenService;
using Microsoft.Extensions.Logging;

namespace ReservationService.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private readonly IReservationRepository _repository;
    private readonly IHotelRepository _hotelRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ReservationsController> _logger;
    private readonly ITokenService _tokenService;

    public ReservationsController(IReservationRepository repository, IHotelRepository hotelRepository, 
        IMapper mapper, ITokenService tokenService, ILogger<ReservationsController> logger)
    {
        _repository = repository;
        _hotelRepository = hotelRepository;
        _mapper = mapper;
        _tokenService = tokenService;
        _logger = logger;
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
            
            return Ok(_mapper.Map<IEnumerable<ReservationResponse>>(reservations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservations for user: {Username}", username);
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

            return Ok(reservation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reservation for user: {Username}. HotelId: {HotelId}", 
                username, reservationRequest.HotelId);
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
                return NotFound();
            }

            var newModel = _mapper.Map<Reservation>(reservationResponse);
            newModel.Id = reservation.Id;

            await _repository.UpdateAsync(newModel, reservation.Id);

            _logger.LogInformation("Reservation updated successfully. UID: {ReservationUid}, Old Status: {OldStatus}, New Status: {NewStatus}", 
                reservationResponse.ReservationUid, reservation.Status, reservationResponse.Status);

            return Ok(newModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reservation for user: {Username}. UID: {ReservationUid}", 
                username, reservationResponse.ReservationUid);
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
                return NotFound();
            }

            _logger.LogInformation("Hotel found: ID={HotelId}, Name={HotelName}", id, hotel.Name);
            
            return Ok(_mapper.Map<HotelResponse>(hotel));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hotel. ID: {HotelId}", id);
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
                return NotFound();
            }

            _logger.LogInformation("Reservation found: UID={ReservationUid}, Status={Status}", 
                uid, reservation.Status);
            
            return Ok(_mapper.Map<ReservationResponse>(reservation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reservation for user: {Username}. UID: {ReservationUid}", 
                username, uid);
            return StatusCode(500, "Internal server error");
        }
    }
}