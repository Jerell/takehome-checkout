using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumber = "4111111111111111", ExpiryMonth = 12, ExpiryYear = 2030,
        Currency = "GBP", Amount = 100, Cvv = "123"
    };

    [Theory]
    [InlineData("123")]
    [InlineData("41111111111111111234")]
    [InlineData("4111-1111-1111-111a")]
    public async Task RejectsInvalidCardNumber(string cardNumber) 
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        var req = ValidRequest();
        req.CardNumber = cardNumber;

        // Act
        var response = await client.PostAsJsonAsync("api/Payments", req);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task RejectsInvalidExpiryMonth(int expiryMonth)
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        var req = ValidRequest();
        req.ExpiryMonth = expiryMonth;

        // Act
        var response = await client.PostAsJsonAsync("api/Payments", req);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10000)]
    public async Task RejectsInvalidExpiryYear(int expiryYear)
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        var req = ValidRequest();
        req.ExpiryYear = expiryYear;

        // Act
        var response = await client.PostAsJsonAsync("api/Payments", req);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(2, 2020)]
    [InlineData(3, 2025)]
    public async Task RejectsPastExpiry(int expiryMonth, int expiryYear)
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        var req = ValidRequest();
        req.ExpiryMonth = expiryMonth;
        req.ExpiryYear = expiryYear;

        // Act
        var response = await client.PostAsJsonAsync("api/Payments", req);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("AUD")]
    [InlineData("JPY")]
    public async Task RejectsUnknownCurrency(string currency)
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        var req = ValidRequest();
        req.Currency = currency;

        // Act
        var response = await client.PostAsJsonAsync("api/Payments", req);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RejectsInvalidAmount(int amount)
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        var req = ValidRequest();
        req.Amount = amount;

        // Act
        var response = await client.PostAsJsonAsync("api/Payments", req);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("12")]      // too short
    [InlineData("12345")]   // too long
    [InlineData("12a")]
    public async Task RejectsInvalidCvv(string cvv)
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        var req = ValidRequest();
        req.Cvv = cvv;

        // Act
        var response = await client.PostAsJsonAsync("api/Payments", req);
        
        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private class FakeBank : IBankService
    {
        private readonly BankAuthorization _result;
        public FakeBank(bool authorized) => _result = new BankAuthorization(authorized, "auth-123");

        public Task<BankAuthorization> AuthorizeAsync(
                PostPaymentRequest req, CancellationToken ct
               )
        {
            return Task.FromResult(_result);
        }
    }

    [Fact]
    public async Task AcceptsValidPayment()
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder =>
                    builder.ConfigureServices(services =>
                        services.AddSingleton<IBankService>(new FakeBank(true))
                        )
                    )
            .CreateClient();

        var request = ValidRequest();
        var response = await webApplicationFactory
            .PostAsJsonAsync("api/Payments", request);
        var created = (await response.Content.ReadFromJsonAsync<PostPaymentResponse>())!;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(PaymentStatus.Authorized, created.Status);
        Assert.Equal("1111", created.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, created.ExpiryMonth);
        Assert.Equal(request.Currency, created.Currency);
        Assert.Equal(request.Amount, created.Amount);

        var getResponse = await webApplicationFactory.GetAsync($"/api/Payments/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<GetPaymentResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created.Id, fetched?.Id);
        Assert.Equal(PaymentStatus.Authorized, fetched?.Status);
    }

    [Fact]
    public async Task ReturnsDeclinedWhenBankDeclines()
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder =>
                    builder.ConfigureServices(services =>
                        services.AddSingleton<IBankService>(new FakeBank(false))
                        )
                    )
            .CreateClient();

        var request = ValidRequest();
        request.CardNumber = "4111111111111112";

        var response = await webApplicationFactory.PostAsJsonAsync("api/Payments", request);
        var body = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(PaymentStatus.Declined, body?.Status);
    }
}