using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly IBankService _bankService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
            PaymentsRepository paymentsRepository,
            IBankService bankService,
            ILogger<PaymentsController> logger
            )
    {
        _paymentsRepository = paymentsRepository;
        _bankService = bankService;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment is null) {
            return new NotFoundResult();
        }

        return new OkObjectResult(new GetPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        });
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync(
            PostPaymentRequest paymentRequest,
            CancellationToken ct
            )
    {
        BankAuthorization authorization;

        try {
            authorization = await _bankService.AuthorizeAsync(paymentRequest, ct);
        }
        catch (HttpRequestException) { // service unavailable or 503 if CardNumber ends with 0
            var rejected = BuildPayment(paymentRequest, PaymentStatus.Rejected);
            _paymentsRepository.Add(rejected);
            _logger.LogInformation(
                "Payment {PaymentId} stored as {Status} after bank failure ({Amount} {Currency}, card ending {CardEnding})",
                rejected.Id, rejected.Status, rejected.Amount, rejected.Currency, rejected.CardNumberLastFour);
            PaymentMetrics.Record(rejected.Status);
            return StatusCode(StatusCodes.Status502BadGateway, ToResponse(rejected));
        }

        var status = authorization.Authorized
            ? PaymentStatus.Authorized
            : PaymentStatus.Declined;

        var payment = BuildPayment(paymentRequest, status);
        _paymentsRepository.Add(payment);
        _logger.LogInformation(
            "Payment {PaymentId} stored as {Status} ({Amount} {Currency}, card ending {CardEnding})",
            payment.Id, payment.Status, payment.Amount, payment.Currency, payment.CardNumberLastFour);
        PaymentMetrics.Record(payment.Status);
         
        return new CreatedResult($"/api/Payments/{payment.Id}", ToResponse(payment));
    }

    private static Payment BuildPayment(
            PostPaymentRequest request,
            PaymentStatus status
            ) => new()
    {
        Id = Guid.NewGuid(),
        Status = status,
        CardNumberLastFour = request.CardNumber[^4..],
        ExpiryMonth = request.ExpiryMonth,
        ExpiryYear = request.ExpiryYear,
        Currency = request.Currency,
        Amount = request.Amount
    };

    private static PostPaymentResponse ToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        Status = payment.Status,
        CardNumberLastFour = payment.CardNumberLastFour,
        ExpiryMonth = payment.ExpiryMonth,
        ExpiryYear = payment.ExpiryYear,
        Currency = payment.Currency,
        Amount = payment.Amount
    };
}