using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;

namespace NaPoso.Tests.Unit;

public class PaymentTransactionServiceExtendedTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PaymentTransactionService _service;

    public PaymentTransactionServiceExtendedTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new PaymentTransactionService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetByUserIdAsync_ReturnsTransactionsOrderedDescending()
    {
        var userId = Guid.NewGuid().ToString();

        _context.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                UserId = userId, OglasId = 1, StripePaymentIntentId = "pi_1",
                StripeEventId = "evt_1", Amount = 100, Currency = "usd",
                Status = PaymentStatus.Paid, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PaymentTransaction
            {
                UserId = userId, OglasId = 2, StripePaymentIntentId = "pi_2",
                StripeEventId = "evt_2", Amount = 200, Currency = "usd",
                Status = PaymentStatus.Paid, CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PaymentTransaction
            {
                UserId = userId, OglasId = 3, StripePaymentIntentId = "pi_3",
                StripeEventId = "evt_3", Amount = 300, Currency = "usd",
                Status = PaymentStatus.Paid, CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetByUserIdAsync(userId);

        Assert.Equal(3, result.Count);
        Assert.Equal(200, result[0].Amount);
        Assert.Equal(300, result[1].Amount);
        Assert.Equal(100, result[2].Amount);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsEmpty_WhenNoTransactions()
    {
        var result = await _service.GetByUserIdAsync("nonexistent_user");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByOglasIdAsync_ReturnsTransactionsForOglas()
    {
        var targetOglasId = 42;

        _context.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                UserId = "u1", OglasId = targetOglasId, StripePaymentIntentId = "pi_for_oglas",
                StripeEventId = "evt_for_oglas", Amount = 500, Currency = "usd",
                Status = PaymentStatus.Paid
            },
            new PaymentTransaction
            {
                UserId = "u2", OglasId = 99, StripePaymentIntentId = "pi_other_oglas",
                StripeEventId = "evt_other_oglas", Amount = 600, Currency = "eur",
                Status = PaymentStatus.Paid
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetByOglasIdAsync(targetOglasId);

        Assert.Single(result);
        Assert.Equal("pi_for_oglas", result[0].StripePaymentIntentId);
    }

    [Fact]
    public async Task GetByOglasIdAsync_ReturnsEmpty_WhenNoTransactions()
    {
        var result = await _service.GetByOglasIdAsync(9999);

        Assert.Empty(result);
    }

    [Fact]
    public async Task IsPaidAsync_ReturnsFalse_WhenStatusIsPending()
    {
        var userId = "user_pending";
        var oglasId = 10;

        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = userId, OglasId = oglasId, StripePaymentIntentId = "pi_pending",
            StripeEventId = "evt_pending", Amount = 100, Currency = "usd",
            Status = PaymentStatus.Pending
        });
        await _context.SaveChangesAsync();

        var result = await _service.IsPaidAsync(userId, oglasId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPaidAsync_ReturnsFalse_WhenStatusIsFailed()
    {
        var userId = "user_failed";
        var oglasId = 11;

        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = userId, OglasId = oglasId, StripePaymentIntentId = "pi_failed",
            StripeEventId = "evt_failed", Amount = 100, Currency = "usd",
            Status = PaymentStatus.Failed
        });
        await _context.SaveChangesAsync();

        var result = await _service.IsPaidAsync(userId, oglasId);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPaidAsync_ReturnsTrue_WhenMultiplePaidExist()
    {
        var userId = "user_multi";
        var oglasId = 12;

        _context.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                UserId = userId, OglasId = oglasId, StripePaymentIntentId = "pi_first",
                StripeEventId = "evt_first", Amount = 100, Currency = "usd",
                Status = PaymentStatus.Paid
            },
            new PaymentTransaction
            {
                UserId = userId, OglasId = oglasId, StripePaymentIntentId = "pi_second",
                StripeEventId = "evt_second", Amount = 200, Currency = "usd",
                Status = PaymentStatus.Paid
            }
        );
        await _context.SaveChangesAsync();

        var result = await _service.IsPaidAsync(userId, oglasId);

        Assert.True(result);
    }
}
