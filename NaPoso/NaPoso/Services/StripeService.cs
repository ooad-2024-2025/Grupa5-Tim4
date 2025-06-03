using Stripe;
using Stripe.Checkout;
using Microsoft.AspNetCore.Http;

namespace NaPoso.Services;

public class StripeService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public StripeService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Session> CreateCheckoutSessionAsync(string productName, long amount, string currency = "usd")
    {
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
            // Updated URLs to match Areas/Identity structure
            SuccessUrl = $"{domain}/Identity/Payment/Success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{domain}/Identity/Payment/Cancel"
        };

        var service = new SessionService();
        return await service.CreateAsync(options);
    }

    public async Task<Session> GetSessionAsync(string sessionId)
    {
        var service = new SessionService();
        return await service.GetAsync(sessionId);
    }
}