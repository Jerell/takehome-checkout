using PaymentGateway.Api.Models.Requests;

public class BankServiceTests
{
    private static BankService RealBank() => new(new HttpClient { BaseAddress = new Uri("http://localhost:8080") });

    private static PostPaymentRequest Request(string cardNumber) => new()
    {
        CardNumber = cardNumber,
        ExpiryMonth = 12,
        ExpiryYear = 2030,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    [Fact]
    public async Task AuthorizesCardEndingInOdd()
    {
        // Arrange
        var bank = RealBank();

        // Act
        var result = await bank.AuthorizeAsync(
                Request("4111111111111111"),
                CancellationToken.None
        );

        // Assert
        Assert.True(result.Authorized);
        Assert.False(string.IsNullOrEmpty(result.AuthorizationCode));
    }

    [Fact]
    public async Task DeclinesCardEndingInEven()
    {
        // Arrange
        var bank = RealBank();

        // Act
        var result = await bank.AuthorizeAsync(
                Request("4111111111111112"),
                CancellationToken.None
        );

        // Assert
        Assert.False(result.Authorized);
    }

    [Fact]
    public async Task RejectsCardEndingInZero()
    {
        // Arrange
        var bank = RealBank();

        // Act
        var action = () => bank.AuthorizeAsync(
                Request("4111111111111110"),
                CancellationToken.None
        );

        // Assert
        await Assert.ThrowsAsync<HttpRequestException>(action);
    }
}