using PaymentGateway.Api.Models.Requests;

public class BankService : IBankService
{
    public Task<BankAuthorization> AuthorizeAsync(
            PostPaymentRequest req,
            CancellationToken ct
        )
    {
        throw new NotImplementedException();
    }
}