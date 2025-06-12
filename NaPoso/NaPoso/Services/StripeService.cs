using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.Http;

namespace NaPoso.Services;

public class StripeService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _apiKey;

    public StripeService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

        _apiKey = _configuration["Stripe:SecretKey"]                   
               ?? _configuration["StripeConfiguration:ApiKey"]         
               ?? _configuration.GetSection("Stripe")["SecretKey"]     
               ?? "sk_test_51RVfSKLFbAyjmhpVLOpSXjsFZkdIimljUSH346uf4xLgFmAUL0G8eQV9d8j4EZNtYcT5dfuQQ1eHFGaZsNVEG1xc00n4XyFvjY"; // Fallback

        StripeConfiguration.ApiKey = _apiKey;
    }

    public async Task<Session> CreateCheckoutSessionAsync(string productName, long amount, string currency = "usd")
    {
        StripeConfiguration.ApiKey = _apiKey;

        if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
        {
            throw new InvalidOperationException("Stripe API key is not configured");
        }

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
                        UnitAmount = amount, // Amount in cents
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
            CancelUrl = $"{domain}/Identity/Payment/Cancel"
        };

        var client = new StripeClient(_apiKey);
        var service = new SessionService(client);
        return await service.CreateAsync(options);
    }

    public async Task<Session> GetSessionAsync(string sessionId)
    {
        StripeConfiguration.ApiKey = _apiKey;

        if (string.IsNullOrEmpty(StripeConfiguration.ApiKey))
        {
            throw new InvalidOperationException("Stripe API key is not configured");
        }

        var client = new StripeClient(_apiKey);
        var service = new SessionService(client);
        return await service.GetAsync(sessionId);
    }
}