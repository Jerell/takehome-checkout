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
            CardNumberLastFour = _random.Next(1111, 9999),
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

}