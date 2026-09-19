using PaymentGateway.Api.Models.Requests;

public class BankService : IBankService
{
    private readonly HttpClient _httpClient;
    public BankService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<BankAuthorization> AuthorizeAsync(
            PostPaymentRequest req,
            CancellationToken ct
        )
    {
        throw new NotImplementedException();
    }
}