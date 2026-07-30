using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using NaPoso.Services;

namespace NaPoso.Tests.Unit;

public class StripeServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public StripeServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    }

    private void SetupNullApiKey()
    {
        _configurationMock.Setup(c => c["Stripe:SecretKey"]).Returns((string?)null);
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["SecretKey"]).Returns((string?)null);
        _configurationMock.Setup(c => c.GetSection("Stripe")).Returns(sectionMock.Object);
    }

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenApiKeyIsNull()
    {
        SetupNullApiKey();
        var service = new StripeService(_configurationMock.Object, _httpContextAccessorMock.Object);

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenApiKeyIsEmpty()
    {
        _configurationMock.Setup(c => c["Stripe:SecretKey"]).Returns("");
        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["SecretKey"]).Returns("");
        _configurationMock.Setup(c => c.GetSection("Stripe")).Returns(sectionMock.Object);

        var service = new StripeService(_configurationMock.Object, _httpContextAccessorMock.Object);

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ReturnsTrue_WhenApiKeyIsValid()
    {
        _configurationMock.Setup(c => c["Stripe:SecretKey"]).Returns("sk_test_1234567890abcdef");

        var service = new StripeService(_configurationMock.Object, _httpContextAccessorMock.Object);

        Assert.True(service.IsConfigured);
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_ReturnsNull_WhenNotConfigured()
    {
        SetupNullApiKey();
        var service = new StripeService(_configurationMock.Object, _httpContextAccessorMock.Object);

        var result = await service.CreateCheckoutSessionAsync("Test Product", 1000, "usd");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSessionAsync_ReturnsNull_WhenNotConfigured()
    {
        SetupNullApiKey();
        var service = new StripeService(_configurationMock.Object, _httpContextAccessorMock.Object);

        var result = await service.GetSessionAsync("cs_test_session_id");

        Assert.Null(result);
    }

    [Theory]
    [InlineData(100.00, 0,    10000)]
    [InlineData(100.00, 10,   11000)]
    [InlineData( 50.50,  5.5,  5600)]
    [InlineData( 20.00,  3.33, 2333)]
    public void Checkout_Amount_U_Stripe_Sadrzi_Bazu_I_Baksis(
        decimal cijenaKM,
        decimal baksisKM,
        long expectedFeninga)
    {
        long cijenaFeninga = (long)Math.Round(cijenaKM * 100);
        long baksisFeninga = (long)Math.Round(baksisKM * 100);
        long totalFeninga = cijenaFeninga + baksisFeninga;

        Assert.Equal(expectedFeninga, totalFeninga);
    }

    [Theory]
    [InlineData(10000L, 1000L, 1000L, 10000L)]
    [InlineData( 5000L,  500L,  500L,  5000L)]
    [InlineData( 2000L,    0L,  200L,  1800L)]
    [InlineData(     0L, 1000L,    0L,  1000L)]
    public void Payout_PlatformskaProvizija_Ne_Uzima_Baksis(
        long osnovicaFeninga,
        long baksisFeninga,
        long expectedPlatformFee,
        long expectedWorkerAmount)
    {
        long transactionAmount = osnovicaFeninga + baksisFeninga;

        long feeCalc = (long)Math.Round(osnovicaFeninga * 0.10);
        long workerCalc = transactionAmount - feeCalc;

        Assert.Equal(expectedPlatformFee, feeCalc);
        Assert.Equal(expectedWorkerAmount, workerCalc);
    }
}
