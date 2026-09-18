using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest : IValidatableObject
{
    [Required, StringLength(19, MinimumLength = 14), RegularExpression(@"^\d+$")]
    public required string CardNumber { get; set; }
    [Range(1, 12)]
    public required int ExpiryMonth { get; set; }
    [Range(1, 9999)]
    public required int ExpiryYear { get; set; }
    [Required, StringLength(3, MinimumLength = 3)]
    public required string Currency { get; set; }
    [Range(1, int.MaxValue)]
    public required int Amount { get; set; }
    [Required, StringLength(4, MinimumLength = 3), RegularExpression(@"^\d+$")]
    public required string Cvv { get; set; }

    private static readonly HashSet<string> AllowedCurrencies =
        new(StringComparer.Ordinal) { "GBP", "USD", "EUR" };

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (!AllowedCurrencies.Contains(Currency)) {
            yield return new ValidationResult("Unsupported currency.", [nameof(Currency)]);
        }

        // if the month is outside the range 1-12, this throws before the range validation so we need an early return
        if (ExpiryMonth < 1 || ExpiryMonth > 12) yield break;
        // similar for year
        if (ExpiryYear < 1 || ExpiryYear > 9999) yield break;

        //check expiry is in the future

        // + 1 month - 1 tick because cards are valid until the end of the expiry month
        var expiry = new DateTime(ExpiryYear, ExpiryMonth, 1).AddMonths(1).AddTicks(-1);
        if (expiry <= DateTime.UtcNow) {
            yield return new ValidationResult(
                    "Expiry must be in the future.", 
                    [nameof(ExpiryMonth), nameof(ExpiryYear)]
                );
        }
    }
}