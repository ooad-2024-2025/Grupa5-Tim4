using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.Http;

namespace NaPoso.Services;

public class StripeService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string? _apiKey;

    public StripeService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        _apiKey = _configuration["Stripe:SecretKey"]
               ?? _configuration.GetSection("Stripe")["SecretKey"];

        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            StripeConfiguration.ApiKey = _apiKey;
        }
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<Session?> CreateCheckoutSessionAsync(
        string productName,
        long amount,
        string currency = "usd",
        Dictionary<string, string>? metadata = null)
    {
        if (!IsConfigured)
            return null;

        var request = _httpContextAccessor.HttpContext.Request;
        var domain = $"{request.Scheme}://{request.Host}";

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = amount,
                        Currency = currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = productName
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = $"{domain}/Identity/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{domain}/Identity/Payment/Cancel",
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        var client = new StripeClient(_apiKey!);
        var service = new SessionService(client);
        return await service.CreateAsync(options);
    }

    public async Task<Session?> GetSessionAsync(string sessionId)
    {
        if (!IsConfigured)
            return null;

        var client = new StripeClient(_apiKey!);
        var service = new SessionService(client);
        return await service.GetAsync(sessionId);
    }
}
