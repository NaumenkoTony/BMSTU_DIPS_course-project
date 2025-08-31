namespace PaymentService.Controllers;

using AutoMapper;
using Contracts.Dto;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Data;
using PaymentService.Models.DomainModels;
using Microsoft.Extensions.Logging;
using PaymentService.Services;


public class PaymentsController : Controller
{
    private readonly IPaymentRepository _repository;
    private readonly IMapper _mapper;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentRepository repository, IMapper mapper, IKafkaProducer kafkaProducer, ILogger<PaymentsController> logger)
    {
        _repository = repository;
        _mapper = mapper;
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
                        Service: "Payment",
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

    [Route("api/v1/[controller]/{uid}")]
    [HttpGet]
    public async Task<ActionResult<PaymentResponse>> GetAsync(string uid)
    {   
        _logger.LogInformation("Get payment request for UID: {PaymentUid}", uid);

        try
        {
            var payment = await _repository.GetByUidAsync(uid);
            
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for UID: {PaymentUid}", uid);
                await PublishUserActionAsync(
                    action: "PaymentViewed",
                    status: "NotFound",
                    metadata: new Dictionary<string, object>
                    {
                        ["PaymentUid"] = uid,
                        ["Error"] = "Payment not found"
                    }
                );
                return NotFound();
            }

            _logger.LogInformation("Payment found: UID={PaymentUid}, Status={Status}, Price={Price}",
                uid, payment.Status, payment.Price);

            await PublishUserActionAsync(
                action: "PaymentViewed",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["PaymentUid"] = payment.PaymentUid,
                    ["Status"] = payment.Status,
                    ["Amount"] = payment.Price,
                }
                );

            return Ok(_mapper.Map<PaymentResponse>(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for UID: {PaymentUid}", uid);
            return StatusCode(500, "Internal server error");
        }
    }

    [Route("api/v1/[controller]")]
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] PaymentRequest paymentRequest)
    {
        _logger.LogInformation("Create payment request. Status: {Status}, Price: {Price}", 
            paymentRequest.Status, paymentRequest.Price);

        try
        {
            var payment = _mapper.Map<Payment>(paymentRequest);
            
            await _repository.CreateAsync(payment);

            var paymentResponse = _mapper.Map<PaymentResponse>(payment);

            _logger.LogInformation("Payment created successfully. UID: {PaymentUid}, ID: {PaymentId}", 
                paymentResponse.PaymentUid, paymentResponse.Id);
            
            await PublishUserActionAsync(
                action: "PaymentCreated",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["PaymentUid"] = paymentResponse.PaymentUid,
                    ["Status"] = paymentResponse.Status,
                    ["Amount"] = paymentResponse.Price,
                    ["CreatedDate"] = DateTime.UtcNow
                }
            );

            return Ok(paymentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment. Status: {Status}, Price: {Price}", 
                paymentRequest.Status, paymentRequest.Price);

            await PublishUserActionAsync(
            action: "PaymentCreated",
            status: "Failed",
            metadata: new Dictionary<string, object>
            {
                ["Amount"] = paymentRequest.Price,
                ["Error"] = ex.Message
            });

            return StatusCode(500, "Internal server error");
        }
    }

    [Route("/api/v1/[controller]")]
    [HttpPut]
    public async Task<ActionResult<PaymentResponse>> UpdatePaymentStatusAsync([FromBody] PaymentResponse paymentResponse)
    {
        _logger.LogInformation("Update payment request. UID: {PaymentUid}, New Status: {Status}", 
            paymentResponse.PaymentUid, paymentResponse.Status);

        try
        {
            var payment = await _repository.GetByUidAsync(paymentResponse.PaymentUid);
            
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for update. UID: {PaymentUid}", paymentResponse.PaymentUid);

                await PublishUserActionAsync(
                    action: "PaymentUpdated",
                    status: "NotFound",
                    metadata: new Dictionary<string, object>
                    {
                        ["PaymentUid"] = paymentResponse.PaymentUid,
                        ["Error"] = "Payment not found for update"
                    }
                );

                return NotFound();
            }
            
            var oldStatus = payment.Status;
            var newModel = _mapper.Map<Payment>(paymentResponse);
            newModel.Id = payment.Id;

            await _repository.UpdateAsync(newModel, payment.Id);

            _logger.LogInformation("Payment updated successfully. UID: {PaymentUid}, Old Status: {OldStatus}, New Status: {NewStatus}", 
                paymentResponse.PaymentUid, payment.Status, paymentResponse.Status);
            
            await PublishUserActionAsync(
                action: "PaymentUpdated",
                status: "Success",
                metadata: new Dictionary<string, object>
                {
                    ["PaymentUid"] = paymentResponse.PaymentUid,
                    ["OldStatus"] = oldStatus,
                    ["NewStatus"] = paymentResponse.Status,
                    ["Amount"] = paymentResponse.Price,
                    ["UpdatedDate"] = DateTime.UtcNow
                }
            );

            return Ok(newModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment. UID: {PaymentUid}", paymentResponse.PaymentUid);

            await PublishUserActionAsync(
                action: "PaymentUpdated",
                status: "Failed",
                metadata: new Dictionary<string, object>
                {
                    ["PaymentUid"] = paymentResponse.PaymentUid,
                    ["NewStatus"] = paymentResponse.Status,
                    ["Error"] = ex.Message
                }
            );
            
            return StatusCode(500, "Internal server error");
        }
    }
}