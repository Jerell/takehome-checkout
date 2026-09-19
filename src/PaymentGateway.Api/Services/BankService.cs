using System.Text.Json.Serialization;
using PaymentGateway.Api.Models.Requests;

public class BankService : IBankService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BankService> _logger;

    public BankService(HttpClient httpClient, ILogger<BankService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BankAuthorization> AuthorizeAsync(
            PostPaymentRequest req,
            CancellationToken ct
        )
    {
        var payload = new
        {
            card_number = req.CardNumber,
            expiry_date = $"{req.ExpiryMonth:D2}/{req.ExpiryYear}",
            currency = req.Currency,
            amount = req.Amount,
            cvv = req.Cvv
        };

        var response = await _httpClient.PostAsJsonAsync("/payments", payload, ct);
        if (!response.IsSuccessStatusCode) {
            _logger.LogError(
                "Bank returned {StatusCode} for card ending {CardEnding} ({Amount} {Currency})",
                (int)response.StatusCode, req.CardNumber[^4..], req.Amount, req.Currency);

            throw new HttpRequestException(
                $"Bank simulator returned {(int)response.StatusCode}");
        }

        var bankResponse = await response.Content
            .ReadFromJsonAsync<BankSimulatorResponse>(cancellationToken: ct);

        return new BankAuthorization(bankResponse!.Authorized, bankResponse.AuthorizationCode);
    }

    private record BankSimulatorResponse(
            bool Authorized,
            [property: JsonPropertyName("authorization_code")]
            string? AuthorizationCode
            );
}