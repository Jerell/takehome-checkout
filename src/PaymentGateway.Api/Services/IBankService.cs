using PaymentGateway.Api.Models.Requests;

public interface IBankService
{
    Task<BankAuthorization> AuthorizeAsync(
            PostPaymentRequest req, 
            CancellationToken ct
        );
}