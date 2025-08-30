namespace PaymentService.Controllers;

using AutoMapper;
using Contracts.Dto;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Data;
using PaymentService.Models.DomainModels;
using Microsoft.Extensions.Logging;

public class PaymentsController : Controller
{
    private readonly IPaymentRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentRepository repository, IMapper mapper, ILogger<PaymentsController> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
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
                return NotFound();
            }

            _logger.LogInformation("Payment found: UID={PaymentUid}, Status={Status}, Price={Price}", 
                uid, payment.Status, payment.Price);
            
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

            return Ok(paymentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment. Status: {Status}, Price: {Price}", 
                paymentRequest.Status, paymentRequest.Price);
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
                return NotFound();
            }

            var newModel = _mapper.Map<Payment>(paymentResponse);
            newModel.Id = payment.Id;

            await _repository.UpdateAsync(newModel, payment.Id);

            _logger.LogInformation("Payment updated successfully. UID: {PaymentUid}, Old Status: {OldStatus}, New Status: {NewStatus}", 
                paymentResponse.PaymentUid, payment.Status, paymentResponse.Status);

            return Ok(newModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment. UID: {PaymentUid}", paymentResponse.PaymentUid);
            return StatusCode(500, "Internal server error");
        }
    }
}