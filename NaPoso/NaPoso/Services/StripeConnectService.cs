using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using Stripe;

namespace NaPoso.Services;

public interface IStripeConnectService
{
    bool IsConfigured { get; }
    Task<string?> CreateExpressAccountAsync(string userId, string email);
    Task<string?> CreateAccountLinkAsync(string accountId, string returnUrl, string refreshUrl);
    Task<Account?> GetAccountAsync(string accountId);
    Task<Transfer?> CreateTransferAsync(string destinationAccountId, long amount, string currency, string? sourceTransaction = null);
    Task<bool> UpdateAccountStatusAsync(string accountId);
}

public class StripeConnectService : IStripeConnectService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StripeConnectService> _logger;
    private readonly string? _apiKey;

    public StripeConnectService(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<StripeConnectService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;

        _apiKey = _configuration["Stripe:SecretKey"]
               ?? _configuration.GetSection("Stripe")["SecretKey"];
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>
    /// Creates a Stripe Express account for a worker (Radnik).
    /// </summary>
    public async Task<string?> CreateExpressAccountAsync(string userId, string email)
    {
        if (!IsConfigured) return null;

        try
        {
            var client = new StripeClient(_apiKey!);
            var service = new AccountService(client);

            var options = new AccountCreateOptions
            {
                Type = "express",
                Email = email,
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions
                    {
                        Requested = true
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "naposo_user_id", userId }
                }
            };

            var account = await service.CreateAsync(options);

            // Save to DB
            var korisnik = await _context.Users.FindAsync(userId);
            if (korisnik is Korisnik k)
            {
                k.StripeConnectedAccountId = account.Id;
                k.StripeOnboardingCompleted = false;
                k.PayoutsEnabled = false;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation(
                "Created Stripe Express account {AccountId} for user {UserId}",
                account.Id, userId);

            return account.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe Express account for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Generates an Account Link for Stripe hosted onboarding.
    /// </summary>
    public async Task<string?> CreateAccountLinkAsync(string accountId, string returnUrl, string refreshUrl)
    {
        if (!IsConfigured) return null;

        try
        {
            var client = new StripeClient(_apiKey!);
            var service = new AccountLinkService(client);

            var options = new AccountLinkCreateOptions
            {
                Account = accountId,
                ReturnUrl = returnUrl,
                RefreshUrl = refreshUrl,
                Type = "account_onboarding"
            };

            var link = await service.CreateAsync(options);
            return link.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create account link for {AccountId}", accountId);
            return null;
        }
    }

    /// <summary>
    /// Retrieves account details from Stripe.
    /// </summary>
    public async Task<Account?> GetAccountAsync(string accountId)
    {
        if (!IsConfigured) return null;

        try
        {
            var client = new StripeClient(_apiKey!);
            var service = new AccountService(client);
            return await service.GetAsync(accountId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe account {AccountId}", accountId);
            return null;
        }
    }

    /// <summary>
    /// Updates the local DB record with the latest Stripe account status.
    /// Called from webhook or manually.
    /// </summary>
    public async Task<bool> UpdateAccountStatusAsync(string accountId)
    {
        try
        {
            var account = await GetAccountAsync(accountId);
            if (account == null) return false;

            var korisnik = await _context.Set<Korisnik>()
                .FirstOrDefaultAsync(k => k.StripeConnectedAccountId == accountId);

            if (korisnik == null) return false;

            korisnik.PayoutsEnabled = account.PayoutsEnabled;
            korisnik.StripeOnboardingCompleted = account.DetailsSubmitted;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated Stripe account status for {AccountId}: PayoutsEnabled={PayoutsEnabled}, OnboardingCompleted={OnboardingCompleted}",
                accountId, korisnik.PayoutsEnabled, korisnik.StripeOnboardingCompleted);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update account status for {AccountId}", accountId);
            return false;
        }
    }

    /// <summary>
    /// Creates a Transfer from the platform to a connected account (worker payout).
    /// Amount should be the worker's share (total - platform fee).
    /// </summary>
    public async Task<Transfer?> CreateTransferAsync(
        string destinationAccountId,
        long amount,
        string currency,
        string? sourceTransaction = null)
    {
        if (!IsConfigured) return null;

        try
        {
            var client = new StripeClient(_apiKey!);
            var service = new TransferService(client);

            var options = new TransferCreateOptions
            {
                Amount = amount,
                Currency = currency,
                Destination = destinationAccountId,
                Description = "NaPoso — isplata za završeni posao"
            };

            if (!string.IsNullOrEmpty(sourceTransaction))
            {
                options.SourceTransaction = sourceTransaction;
            }

            var transfer = await service.CreateAsync(options);

            _logger.LogInformation(
                "Created Transfer {TransferId} of {Amount} {Currency} to {Destination}",
                transfer.Id, amount, currency, destinationAccountId);

            return transfer;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex,
                "Failed to create transfer to {Destination} for {Amount} {Currency}",
                destinationAccountId, amount, currency);
            return null;
        }
    }
}
