using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;

namespace NaPoso.Tests;

public class PaymentTransactionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PaymentTransactionService _service;

    public PaymentTransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new PaymentTransactionService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetByStripePaymentIntentId_ReturnsTransaction_WhenExists()
    {
        await _context.HandleStripePaymentEventAsync(
            "pi_test_100", "evt_100", PaymentStatus.Paid, 1000, "usd");

        var result = await _service.GetByStripePaymentIntentIdAsync("pi_test_100");

        Assert.NotNull(result);
        Assert.Equal("pi_test_100", result.StripePaymentIntentId);
    }

    [Fact]
    public async Task GetByStripePaymentIntentId_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetByStripePaymentIntentIdAsync("pi_nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task IsPaid_ReturnsTrue_WhenPaidTransactionExists()
    {
        var user = new Korisnik { Id = "user1", UserName = "test@test.com", Email = "test@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = "user1",
            OglasId = 1,
            StripePaymentIntentId = "pi_paid",
            StripeEventId = "evt_paid",
            Amount = 1000,
            Currency = "usd",
            Status = PaymentStatus.Paid
        });
        await _context.SaveChangesAsync();

        var result = await _service.IsPaidAsync("user1", 1);

        Assert.True(result);
    }

    [Fact]
    public async Task IsPaid_ReturnsFalse_WhenNotPaid()
    {
        var result = await _service.IsPaidAsync("user_nonexistent", 999);

        Assert.False(result);
    }
}
