namespace GatewayService.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using Contracts.Dto;
using Newtonsoft.Json;
using AutoMapper;
using System.Text;
using GatewayService.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using GatewayService.Models;
using Microsoft.AspNetCore.Authorization;
using GatewayService.TokenService;


[Authorize]
[ApiController]
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<GatewayController> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly ITokenService _tokenService;

    private readonly CircuitBreaker _reservationServiceCircuitBreaker;
    private readonly CircuitBreaker _loyaltyServiceCircuitBreaker;
    private readonly CircuitBreaker _paymentServiceCircuitBreaker;

    public GatewayController(
        IHttpClientFactory httpClientFactory,
        IMapper mapper,
        ITokenService tokenService,
        ILogger<GatewayController> logger,
        IConnectionMultiplexer redis)
    {
        _httpClientFactory = httpClientFactory;
        _mapper = mapper;
        _tokenService = tokenService;
        _logger = logger;
        _redis = redis;

        _reservationServiceCircuitBreaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));
        _loyaltyServiceCircuitBreaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));
        _paymentServiceCircuitBreaker = new CircuitBreaker(5, TimeSpan.FromSeconds(60));
    }

    private async Task<AggregatedReservationResponse> CreateReservationResponseAsync(ReservationResponse reservationServiceResponse)
    {
        const string methodName = nameof(CreateReservationResponseAsync);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["ReservationUid"] = reservationServiceResponse.ReservationUid
        });

        _logger.LogInformation("Creating aggregated reservation response");

        HotelInfo? hotelInfo = null;
        PaymentInfo? paymentInfo = null;

        try
        {
            var reservationService = _httpClientFactory.CreateClient("ReservationService");
            if (_reservationServiceCircuitBreaker.AllowRequest())
            {
                _logger.LogDebug("Requesting hotel info for HotelId: {HotelId}", reservationServiceResponse.HotelId);

                var hotelResponse = await reservationService.GetAsync($"/api/v1/reservations/hotels/{reservationServiceResponse.HotelId}");

                _logger.LogInformation("Hotel service response: {StatusCode}", hotelResponse.StatusCode);

                if (!hotelResponse.IsSuccessStatusCode)
                {
                    _reservationServiceCircuitBreaker.RecordFailure();
                    _logger.LogWarning("Failed to get hotel info: {StatusCode}", hotelResponse.StatusCode);
                }
                else
                {
                    _reservationServiceCircuitBreaker.RecordSuccess();
                    var hotelJsonString = await hotelResponse.Content.ReadAsStringAsync();
                    var hotel = JsonConvert.DeserializeObject<HotelResponse>(hotelJsonString);
                    hotelInfo = _mapper.Map<HotelInfo>(hotel);

                    _logger.LogDebug("Successfully retrieved hotel info: {HotelName}", hotelInfo?.Name);
                }
            }
            else
            {
                _logger.LogWarning("Circuit breaker blocked hotel info request");
            }
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error retrieving hotel info for HotelId: {HotelId}", reservationServiceResponse.HotelId);
        }

        try
        {
            var paymentService = _httpClientFactory.CreateClient("PaymentService");
            if (_paymentServiceCircuitBreaker.AllowRequest())
            {
                _logger.LogDebug("Requesting payment info for PaymentUid: {PaymentUid}", reservationServiceResponse.PaymentUid);

                var paymentResponse = await paymentService.GetAsync($"/api/v1/payments/{reservationServiceResponse.PaymentUid}");

                _logger.LogInformation("Payment service response: {StatusCode}", paymentResponse.StatusCode);

                if (!paymentResponse.IsSuccessStatusCode)
                {
                    _paymentServiceCircuitBreaker.RecordFailure();
                    _logger.LogWarning("Failed to get payment info: {StatusCode}", paymentResponse.StatusCode);
                }
                else
                {
                    _paymentServiceCircuitBreaker.RecordSuccess();
                    var paymentJsonString = await paymentResponse.Content.ReadAsStringAsync();
                    var payment = JsonConvert.DeserializeObject<PaymentResponse>(paymentJsonString);
                    paymentInfo = _mapper.Map<PaymentInfo>(payment);

                    _logger.LogDebug("Successfully retrieved payment info: {PaymentStatus}", paymentInfo?.Status);
                }
            }
            else
            {
                _logger.LogWarning("Circuit breaker blocked payment info request");
            }
        }
        catch (Exception ex)
        {
            _paymentServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error retrieving payment info for PaymentUid: {PaymentUid}", reservationServiceResponse.PaymentUid);
        }

        var reservationResponse = new AggregatedReservationResponse
        {
            ReservationUid = reservationServiceResponse.ReservationUid,
            Hotel = hotelInfo,
            StartDate = reservationServiceResponse.StartDate,
            EndDate = reservationServiceResponse.EndDate,
            Status = reservationServiceResponse.Status,
            Payment = paymentInfo
        };

        _logger.LogInformation("Aggregated reservation response created successfully");
        return reservationResponse;
    }

    [Route("api/v1/hotels")]
    [HttpGet]
    public async Task<ActionResult<PaginationResponse>> GetHotelsAsync([FromQuery] int page, [FromQuery] int size)
    {
        const string methodName = nameof(GetHotelsAsync);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["Page"] = page,
            ["Size"] = size
        });

        _logger.LogInformation("Getting hotels list");

        if (page < 0 || size < 1)
        {
            _logger.LogWarning("Invalid pagination parameters: Page={Page}, Size={Size}", page, size);
            return BadRequest(new { message = "Invalid pagination parameters" });
        }

        try
        {
            if (!_reservationServiceCircuitBreaker.AllowRequest())
            {
                _logger.LogWarning("Reservation service circuit breaker is open");
                return StatusCode(503, new { message = "Service unavailable. Please try again later." });
            }

            var reservationService = _httpClientFactory.CreateClient("ReservationService");
            var hotelsResponse = await reservationService.GetAsync($"/api/v1/hotels?page={page}&size={size}");

            _logger.LogInformation("Reservation service response: {StatusCode}", hotelsResponse.StatusCode);

            if (!hotelsResponse.IsSuccessStatusCode)
            {
                _reservationServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to get hotels: {StatusCode}", hotelsResponse.StatusCode);
                return StatusCode(503, new { message = "Error receiving hotels from microservice" });
            }

            _reservationServiceCircuitBreaker.RecordSuccess();
            var hotelsJsonString = await hotelsResponse.Content.ReadAsStringAsync();
            var hotelsServiceResponse = JsonConvert.DeserializeObject<PaginatedResponse<HotelResponse>>(hotelsJsonString);

            if (hotelsServiceResponse == null)
            {
                _logger.LogError("Failed to parse hotel data");
                return StatusCode(503, new { message = "Error parsing hotel data" });
            }

            var pagedResponse = new PaginationResponse
            {
                Page = page,
                PageSize = size,
                TotalElements = hotelsServiceResponse.TotalCount,
                Items = _mapper.Map<List<HotelResponse>>(hotelsServiceResponse.Items)
            };

            _logger.LogInformation("Successfully retrieved {Count} hotels", hotelsServiceResponse.Items?.Count() ?? 0);
            return Ok(pagedResponse);
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error getting hotels");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }
    }

    [Route("/api/v1/me")]
    [HttpGet]
    public async Task<ActionResult<UserInfoResponse>> GetInfoAsync()
    {
        const string methodName = nameof(GetInfoAsync);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName
        });

        _logger.LogInformation("Getting user info");

        try
        {
            if (!_reservationServiceCircuitBreaker.AllowRequest())
            {
                _logger.LogWarning("Reservation service circuit breaker is open");
                return StatusCode(503, new { message = "Reservation Service unavailable" });
            }

            var reservationService = _httpClientFactory.CreateClient("ReservationService");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations");
            var reservationsResponse = await reservationService.SendAsync(request);

            _logger.LogInformation("Reservations service response: {StatusCode}", reservationsResponse.StatusCode);

            if (!reservationsResponse.IsSuccessStatusCode)
            {
                _reservationServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to get reservations: {StatusCode}", reservationsResponse.StatusCode);
                return StatusCode((int)reservationsResponse.StatusCode, new { message = "Error receiving reservations from microservice" });
            }

            _reservationServiceCircuitBreaker.RecordSuccess();
            var reservationsJsonString = await reservationsResponse.Content.ReadAsStringAsync();
            var reservations = JsonConvert.DeserializeObject<List<ReservationResponse>>(reservationsJsonString);

            if (reservations == null)
            {
                _logger.LogError("Failed to parse reservation data");
                return StatusCode(503, new { message = "Error parsing reservation data" });
            }

            _logger.LogInformation("Retrieved {Count} reservations", reservations.Count);

            List<AggregatedReservationResponse> reservationResponses = [];
            foreach (var reservation in reservations)
            {
                reservationResponses.Add(await CreateReservationResponseAsync(reservation));
            }

            LoyaltyResponse? loyalty = null;
            try
            {
                if (_loyaltyServiceCircuitBreaker.AllowRequest())
                {
                    var loyaltyService = _httpClientFactory.CreateClient("LoyaltyService");
                    request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties");
                    var loyaltiesResponse = await loyaltyService.SendAsync(request);

                    _logger.LogInformation("Loyalty service response: {StatusCode}", loyaltiesResponse.StatusCode);

                    if (!loyaltiesResponse.IsSuccessStatusCode)
                    {
                        _loyaltyServiceCircuitBreaker.RecordFailure();
                        _logger.LogWarning("Failed to get loyalty: {StatusCode}", loyaltiesResponse.StatusCode);
                        return StatusCode((int)loyaltiesResponse.StatusCode, new { message = "Error receiving loyalty from microservice" });
                    }

                    _loyaltyServiceCircuitBreaker.RecordSuccess();
                    var loyaltiesResponseJsonString = await loyaltiesResponse.Content.ReadAsStringAsync();
                    loyalty = JsonConvert.DeserializeObject<LoyaltyResponse>(loyaltiesResponseJsonString);

                    if (loyalty == null)
                    {
                        _logger.LogWarning("Failed to parse loyalty data");
                        return StatusCode(503, new { message = "Error parsing loyalty data" });
                    }

                    _logger.LogDebug("Successfully retrieved loyalty info");
                }
                else
                {
                    _logger.LogWarning("Loyalty service circuit breaker is open");
                }
            }
            catch (Exception ex)
            {
                _loyaltyServiceCircuitBreaker.RecordFailure();
                _logger.LogError(ex, "Error getting loyalty info");
            }

            UserInfoResponse userInfoResponse = new UserInfoResponse
            {
                Reservations = reservationResponses,
                Loyalty = loyalty
            };

            _logger.LogInformation("User info retrieved successfully");
            return Ok(userInfoResponse);
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error getting user info");
            return StatusCode(503, new { message = "Reservation Service unavailable." });
        }
    }

    [Route("api/v1/loyalty")]
    [HttpGet]
    public async Task<ActionResult<LoyaltyInfoResponse>> GetLoyalty()
    {
        const string methodName = nameof(GetLoyalty);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName
        });

        _logger.LogInformation("Getting loyalty info");

        try
        {
            if (!_loyaltyServiceCircuitBreaker.AllowRequest())
            {
                _logger.LogWarning("Loyalty service circuit breaker is open");
                return StatusCode(503, new { message = "Loyalty Service unavailable" });
            }

            var loyaltyService = _httpClientFactory.CreateClient("LoyaltyService");
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/loyalties");

            var loyaltyResponse = await loyaltyService.SendAsync(request);

            _logger.LogInformation("Loyalty service response: {StatusCode}", loyaltyResponse.StatusCode);

            if (!loyaltyResponse.IsSuccessStatusCode)
            {
                _loyaltyServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to get loyalty: {StatusCode}", loyaltyResponse.StatusCode);
                return StatusCode(503, new { message = "Error receiving loyalties from microservice" });
            }

            _loyaltyServiceCircuitBreaker.RecordSuccess();

            var loyaltyJsonString = await loyaltyResponse.Content.ReadAsStringAsync();
            var loyaltyServiceResponse = JsonConvert.DeserializeObject<LoyaltyResponse>(loyaltyJsonString);

            if (loyaltyServiceResponse == null)
            {
                _loyaltyServiceCircuitBreaker.RecordFailure();
                _logger.LogError("Failed to parse loyalty data");
                return StatusCode(503, new { message = "Error parsing loyalty data" });
            }

            _logger.LogInformation("Loyalty info retrieved successfully");
            return Ok(_mapper.Map<LoyaltyInfoResponse>(loyaltyServiceResponse));
        }
        catch (Exception ex)
        {
            _loyaltyServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error getting loyalty info");
            return StatusCode(503, new { message = "Loyalty Service unavailable" });
        }
    }

    [Route("api/v1/reservations")]
    [HttpPost]
    public async Task<ActionResult<CreateReservationResponse>> BookHotel([FromBody] CreateReservationRequest createReservationRequest)
    {
        const string methodName = nameof(BookHotel);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["HotelUid"] = createReservationRequest.HotelUid
        });

        _logger.LogInformation("Starting hotel booking process");

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state: {ModelState}", ModelState);
            return BadRequest(ModelState);
        }

        if (!_reservationServiceCircuitBreaker.AllowRequest())
        {
            _logger.LogWarning("Reservation service circuit breaker is open");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }

        HotelResponse? hotel;
        HttpClient reservationService;
        try
        {
            reservationService = _httpClientFactory.CreateClient("ReservationService");

            var hotelResponse = await reservationService.GetAsync($"/api/v1/hotels/{createReservationRequest.HotelUid}");

            _logger.LogInformation("Hotel service response: {StatusCode}", hotelResponse.StatusCode);

            if (!hotelResponse.IsSuccessStatusCode)
            {
                _reservationServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to get hotel: {StatusCode}", hotelResponse.StatusCode);
                return StatusCode((int)hotelResponse.StatusCode, new { message = $"Error receiving hotel from microservice" });
            }

            _reservationServiceCircuitBreaker.RecordSuccess();
            var hotelJsonString = await hotelResponse.Content.ReadAsStringAsync();
            hotel = JsonConvert.DeserializeObject<HotelResponse>(hotelJsonString);

            if (null == hotel)
            {
                _logger.LogWarning("Hotel not found with Uid: {HotelUid}", createReservationRequest.HotelUid);
                return NotFound($"Hotel with uid {createReservationRequest.HotelUid} was not found");
            }

            _logger.LogDebug("Retrieved hotel: {HotelName}, Price: {Price}", hotel.Name, hotel.Price);
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error getting hotel info");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }

        DateTime startDate = DateTime.Parse(createReservationRequest.StartDate);
        DateTime endDate = DateTime.Parse(createReservationRequest.EndDate);
        int nightsNumber = (endDate - startDate).Days;
        int cost = nightsNumber * hotel.Price;

        _logger.LogDebug("Booking details: StartDate={StartDate}, EndDate={EndDate}, Nights={Nights}, Cost={Cost}",
            startDate, endDate, nightsNumber, cost);

        if (!_loyaltyServiceCircuitBreaker.AllowRequest())
        {
            _logger.LogWarning("Loyalty service circuit breaker is open");
            return StatusCode(503, new { message = "Loyalty Service unavailable" });
        }

        LoyaltyInfoResponse? loyalty;
        HttpClient loyaltyService;
        try
        {
            loyaltyService = _httpClientFactory.CreateClient("LoyaltyService");

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties");
            var loyaltyResponse = await loyaltyService.SendAsync(request);

            _logger.LogInformation("Loyalty service response: {StatusCode}", loyaltyResponse.StatusCode);

            if (!loyaltyResponse.IsSuccessStatusCode)
            {
                _loyaltyServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to get loyalty: {StatusCode}", loyaltyResponse.StatusCode);
                return StatusCode((int)loyaltyResponse.StatusCode, new { message = "Error receiving loyalties from microservice" });
            }

            _loyaltyServiceCircuitBreaker.RecordSuccess();
            var loyaltyJsonString = await loyaltyResponse.Content.ReadAsStringAsync();
            var loyaltyServiceResponse = JsonConvert.DeserializeObject<LoyaltyResponse>(loyaltyJsonString);

            if (loyaltyServiceResponse == null)
            {
                _logger.LogError("Failed to parse loyalty data");
                return StatusCode(503, new { message = "Error parsing loyalty data" });
            }

            loyalty = _mapper.Map<LoyaltyInfoResponse>(loyaltyServiceResponse);
            _logger.LogDebug("Retrieved loyalty: Discount={Discount}%", loyalty.Discount);
        }
        catch (Exception ex)
        {
            _loyaltyServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error getting loyalty info");
            return StatusCode(503, new { message = "Loyalty Service unavailable" });
        }

        double discount = loyalty.Discount;
        int costWithDiscount = Convert.ToInt32(cost * (100 - discount) / 100);

        _logger.LogDebug("Cost calculation: Original={Cost}, Discount={Discount}%, Final={CostWithDiscount}",
            cost, discount, costWithDiscount);

        var paymentRequest = new PaymentRequest
        {
            Status = PaymentOperationStatus.PAID.ToString(),
            Price = costWithDiscount
        };

        if (!_paymentServiceCircuitBreaker.AllowRequest())
        {
            _logger.LogWarning("Payment service circuit breaker is open");
            return StatusCode(503, new { message = "Payment Service unavailable" });
        }

        PaymentResponse? payment;
        HttpClient paymentClient;
        try
        {
            paymentClient = _httpClientFactory.CreateClient("PaymentService");

            string paymentJsonContent = System.Text.Json.JsonSerializer.Serialize(paymentRequest);
            var paymentContent = new StringContent(paymentJsonContent, Encoding.UTF8, "application/json");
            var paymentResponse = await paymentClient.PostAsync("/api/v1/payments", paymentContent);

            _logger.LogInformation("Payment service response: {StatusCode}", paymentResponse.StatusCode);

            if (!paymentResponse.IsSuccessStatusCode)
            {
                _paymentServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to create payment: {StatusCode}", paymentResponse.StatusCode);
                return StatusCode((int)paymentResponse.StatusCode, new { message = "Error posting payments from microservice" });
            }

            _paymentServiceCircuitBreaker.RecordSuccess();
            var paymentResponseJsonString = await paymentResponse.Content.ReadAsStringAsync();
            payment = JsonConvert.DeserializeObject<PaymentResponse>(paymentResponseJsonString);

            if (null == payment)
            {
                _logger.LogError("Payment creation failed - null response");
                return NotFound($"Payment was not created");
            }

            _logger.LogDebug("Payment created: Uid={PaymentUid}, Status={Status}", payment.PaymentUid, payment.Status);
        }
        catch (Exception ex)
        {
            _paymentServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(503, new { message = "Payment Service unavailable" });
        }

        string username = _tokenService.GetUsernameFromJWT();
        var reservationRequest = new ReservationRequest
        {
            Username = username,
            HotelId = hotel.Id,
            StartDate = startDate.ToString(),
            EndData = endDate.ToString(),
            Status = ReservationStatus.PAID.ToString(),
            PaymentUid = Convert.ToString(payment.PaymentUid)
        };

        var reservationJsonContent = System.Text.Json.JsonSerializer.Serialize(reservationRequest);
        var reservationContent = new StringContent(reservationJsonContent, Encoding.UTF8, "application/json");

        ReservationResponse? reservation;
        try
        {
            reservationService = _httpClientFactory.CreateClient("ReservationService");

            var reservationResponseBody = await reservationService.PostAsync("/api/v1/reservations/", reservationContent);

            _logger.LogInformation("Reservation service response: {StatusCode}", reservationResponseBody.StatusCode);

            if (!reservationResponseBody.IsSuccessStatusCode)
            {
                _reservationServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to create reservation: {StatusCode}", reservationResponseBody.StatusCode);
                return StatusCode((int)reservationResponseBody.StatusCode, new { message = "Error creating reservation from microservice" });
            }

            _reservationServiceCircuitBreaker.RecordSuccess();
            var reservationResponseBodyJsonString = await reservationResponseBody.Content.ReadAsStringAsync();
            reservation = JsonConvert.DeserializeObject<ReservationResponse>(reservationResponseBodyJsonString);

            if (null == reservation)
            {
                _logger.LogError("Reservation creation failed - null response");
                return NotFound($"Reservation was not created");
            }

            _logger.LogDebug("Reservation created: Uid={ReservationUid}", reservation.ReservationUid);
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error creating reservation");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }

        HttpClient loyaltyClient;
        try
        {
            loyaltyClient = _httpClientFactory.CreateClient("LoyaltyService");

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties/improve");
            var loyaltyResponse = await loyaltyService.SendAsync(request);

            _logger.LogInformation("Loyalty improve response: {StatusCode}", loyaltyResponse.StatusCode);

            if (!loyaltyResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to improve loyalty: {StatusCode}", loyaltyResponse.StatusCode);
            }
            else
            {
                _logger.LogDebug("Loyalty improved successfully");
            }
        }
        catch (Exception ex)
        {
            _loyaltyServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error improving loyalty");
            await paymentClient.DeleteAsync($"/api/v1/payments/{payment.PaymentUid}");
            _logger.LogInformation("Payment rolled back due to loyalty error");
            return StatusCode(503, new { message = "Loyalty Service unavailable" });
        }

        var createReservationResponse = new CreateReservationResponse
        {
            ReservationUid = reservation.ReservationUid,
            HotelUid = hotel.HotelUid,
            StartDate = createReservationRequest.StartDate,
            EndDate = createReservationRequest.EndDate,
            Discount = discount,
            Status = paymentRequest.Status,
            Payment = _mapper.Map<PaymentInfo>(payment)
        };

        _logger.LogInformation("Hotel booking completed successfully for user: {Username}", username);
        return Ok(createReservationResponse);
    }

    [Route("api/v1/reservations/{reservationUid}")]
    [HttpGet]
    public async Task<ActionResult<AggregatedReservationResponse>> GetReservationByUsername(string reservationUid)
    {
        const string methodName = nameof(GetReservationByUsername);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["ReservationUid"] = reservationUid
        });

        _logger.LogInformation("Getting reservation by UID");

        try
        {
            if (!_reservationServiceCircuitBreaker.AllowRequest())
            {
                _logger.LogWarning("Reservation service circuit breaker is open");
                return StatusCode(503, new { message = "Reservation Service unavailable" });
            }

            var reservationService = _httpClientFactory.CreateClient("ReservationService");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations/{reservationUid}");
            var reservationServiceResponse = await reservationService.SendAsync(request);

            _logger.LogInformation("Reservation service response: {StatusCode}", reservationServiceResponse.StatusCode);

            if (!reservationServiceResponse.IsSuccessStatusCode)
            {
                _reservationServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to get reservation: {StatusCode}", reservationServiceResponse.StatusCode);
                return StatusCode(503, new { message = $"Error receiving reservation from microservice" });
            }

            _reservationServiceCircuitBreaker.RecordSuccess();
            var reservationJsonString = await reservationServiceResponse.Content.ReadAsStringAsync();
            var reservation = JsonConvert.DeserializeObject<ReservationResponse>(reservationJsonString);

            if (null == reservation)
            {
                _logger.LogWarning("Reservation not found with Uid: {ReservationUid}", reservationUid);
                return NotFound($"Reservations with uid {reservationUid} was not found");
            }

            _logger.LogDebug("Successfully retrieved reservation: {ReservationUid}", reservation.ReservationUid);

            AggregatedReservationResponse reservationResponse = await CreateReservationResponseAsync(reservation);

            _logger.LogInformation("Reservation details retrieved successfully");
            return Ok(reservationResponse);
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error getting reservation by UID");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }
    }

    [Route("api/v1/reservations")]
    [HttpGet]
    public async Task<ActionResult<AggregatedReservationResponse>> GetReservationsByUsername()
    {
        const string methodName = nameof(GetReservationsByUsername);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName
        });

        _logger.LogInformation("Getting all reservations for user");

        try
        {
            if (!_reservationServiceCircuitBreaker.AllowRequest())
            {
                _logger.LogWarning("Reservation service circuit breaker is open");
                return StatusCode(503, new { message = "Reservation Service unavailable" });
            }

            var reservationService = _httpClientFactory.CreateClient("ReservationService");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations");
            var reservationsResponse = await reservationService.SendAsync(request);

            _logger.LogInformation("Reservation service response: {StatusCode}", reservationsResponse.StatusCode);

            if (!reservationsResponse.IsSuccessStatusCode)
            {
                _reservationServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Failed to get reservations: {StatusCode}", reservationsResponse.StatusCode);
                return StatusCode(503, new { message = $"Error receiving reservations from microservice" });
            }

            _reservationServiceCircuitBreaker.RecordSuccess();
            var reservationsJsonString = await reservationsResponse.Content.ReadAsStringAsync();
            var reservations = JsonConvert.DeserializeObject<List<ReservationResponse>>(reservationsJsonString);

            if (reservations == null)
            {
                _logger.LogError("Failed to parse reservation data");
                return StatusCode(503, new { message = "Error parsing reservation data" });
            }

            _logger.LogInformation("Retrieved {Count} reservations", reservations.Count);

            List<AggregatedReservationResponse> reservationResponses = [];
            foreach (var reservation in reservations)
            {
                reservationResponses.Add(await CreateReservationResponseAsync(reservation));
            }

            _logger.LogInformation("All reservations processed successfully");
            return Ok(reservationResponses);
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error getting reservations for user");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }
    }

    [Route("api/v1/reservations/{reservationUid}")]
    [HttpDelete]
    public async Task<ActionResult<AggregatedReservationResponse>> UnbookHotel(string reservationUid)
    {
        const string methodName = nameof(UnbookHotel);
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Method"] = methodName,
            ["ReservationUid"] = reservationUid
        });

        _logger.LogInformation("Starting hotel unbooking process");

        if (!_reservationServiceCircuitBreaker.AllowRequest())
        {
            _logger.LogWarning("Reservation service circuit breaker is open");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }

        ReservationResponse? reservation;
        HttpClient reservationService;
        try
        {
            reservationService = _httpClientFactory.CreateClient("ReservationService");

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reservations/{reservationUid}");
            var reservationServiceResponse = await reservationService.SendAsync(request);

            _logger.LogInformation("Get reservation response: {StatusCode}", reservationServiceResponse.StatusCode);

            if (!reservationServiceResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get reservation: {StatusCode}", reservationServiceResponse.StatusCode);
                return StatusCode((int)reservationServiceResponse.StatusCode, new { message = $"Error receiving reservation from microservice" });
            }

            var reservationJsonString = await reservationServiceResponse.Content.ReadAsStringAsync();
            reservation = JsonConvert.DeserializeObject<ReservationResponse>(reservationJsonString);

            if (null == reservation)
            {
                _logger.LogWarning("Reservation not found with Uid: {ReservationUid}", reservationUid);
                return NotFound($"Reservations with uid {reservationUid} was not found");
            }

            _logger.LogDebug("Retrieved reservation: Status={Status}, PaymentUid={PaymentUid}",
                reservation.Status, reservation.PaymentUid);

            reservation.Status = ReservationStatus.CANCELED.ToString();
            request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/reservations");
            var json = JsonConvert.SerializeObject(reservation);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            reservationServiceResponse = await reservationService.SendAsync(request);

            _logger.LogInformation("Update reservation response: {StatusCode}", reservationServiceResponse.StatusCode);

            if (!reservationServiceResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update reservation: {StatusCode}", reservationServiceResponse.StatusCode);
                return StatusCode((int)reservationServiceResponse.StatusCode, new { message = $"Error updating reservation" });
            }

            _logger.LogInformation("Reservation successfully canceled");
        }
        catch (Exception ex)
        {
            _reservationServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error processing reservation cancellation");
            return StatusCode(503, new { message = "Reservation Service unavailable" });
        }

        if (!_paymentServiceCircuitBreaker.AllowRequest())
        {
            _logger.LogWarning("Payment service circuit breaker is open");
            return StatusCode(503, new { message = "Payment Service unavailable" });
        }

        HttpClient paymentService;
        try
        {
            paymentService = _httpClientFactory.CreateClient("PaymentService");

            var paymentServiceResponse = await paymentService.GetAsync($"/api/v1/payments/{reservation.PaymentUid}");

            _logger.LogInformation("Get payment response: {StatusCode}", paymentServiceResponse.StatusCode);

            var paymentJsonString = await paymentServiceResponse.Content.ReadAsStringAsync();
            var payment = JsonConvert.DeserializeObject<PaymentResponse>(paymentJsonString);

            if (null == payment)
            {
                _paymentServiceCircuitBreaker.RecordFailure();
                _logger.LogWarning("Payment not found with Uid: {PaymentUid}", reservation.PaymentUid);
                return NotFound($"Payment with uid {reservation.PaymentUid} was not found");
            }

            _paymentServiceCircuitBreaker.RecordSuccess();

            _logger.LogDebug("Retrieved payment: Status={Status}", payment.Status);

            payment.Status = PaymentOperationStatus.CANCELED.ToString();
            var request = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/payments");
            var json = JsonConvert.SerializeObject(payment);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            paymentServiceResponse = await paymentService.SendAsync(request);

            _logger.LogInformation("Update payment response: {StatusCode}", paymentServiceResponse.StatusCode);

            if (!paymentServiceResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update payment: {StatusCode}", paymentServiceResponse.StatusCode);
                return StatusCode((int)paymentServiceResponse.StatusCode, new { message = $"Error updating payment" });
            }

            _logger.LogInformation("Payment successfully canceled");
        }
        catch (Exception ex)
        {
            _paymentServiceCircuitBreaker.RecordFailure();
            _logger.LogError(ex, "Error processing payment cancellation");
            return StatusCode(503, new { message = "Payment Service unavailable" });
        }

        var accessToken = _tokenService.GetAccessToken();

        if (!_loyaltyServiceCircuitBreaker.AllowRequest())
        {
            _logger.LogWarning("Loyalty service circuit breaker is open, queuing request");
            await EnqueueLoyaltyRequestAsync(accessToken);
            _logger.LogInformation("Loyalty request queued successfully");
            return NoContent();
        }

        try
        {
            var loyaltyClient = _httpClientFactory.CreateClient("LoyaltyService");
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/loyalties/degrade");
            var loyaltyResponse = await loyaltyClient.SendAsync(request);

            _logger.LogInformation("Loyalty degrade response: {StatusCode}", loyaltyResponse.StatusCode);

            if (!loyaltyResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Loyalty degrade failed: {StatusCode}, queuing request", loyaltyResponse.StatusCode);
                await EnqueueLoyaltyRequestAsync(accessToken);
            }
            else
            {
                _logger.LogInformation("Loyalty successfully degraded");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error degrading loyalty, queuing request");
            await EnqueueLoyaltyRequestAsync(accessToken);
        }

        _logger.LogInformation("Hotel unbooking completed successfully");
        return NoContent();
    }

    private async Task EnqueueLoyaltyRequestAsync(string accessToken)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.ListRightPushAsync("loyalty-queue", accessToken);
            _logger.LogInformation("Loyalty request queued successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue loyalty request");
            throw;
        }
    }
}