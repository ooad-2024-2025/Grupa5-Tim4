using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Tests;

public class PaymentTransactionTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public PaymentTransactionTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task HandleStripePaymentEvent_CreatesNewTransaction_WhenPaymentIntentNotFound()
    {
        await _context.HandleStripePaymentEventAsync(
            "pi_test_123", "evt_test_1", PaymentStatus.Paid, 1000, "usd");

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == "pi_test_123");

        Assert.NotNull(transaction);
        Assert.Equal(PaymentStatus.Paid, transaction.Status);
        Assert.Equal(1000, transaction.Amount);
        Assert.Equal("usd", transaction.Currency);
        Assert.Equal("evt_test_1", transaction.StripeEventId);
        Assert.NotNull(transaction.PaidAt);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_UpdatesExistingTransaction_WhenPaymentIntentExists()
    {
        await _context.HandleStripePaymentEventAsync(
            "pi_test_456", "evt_test_2", PaymentStatus.Pending, 500, "usd");

        await _context.HandleStripePaymentEventAsync(
            "pi_test_456", "evt_test_3", PaymentStatus.Paid, 500, "usd");

        var transactions = await _context.PaymentTransactions
            .Where(p => p.StripePaymentIntentId == "pi_test_456")
            .ToListAsync();

        Assert.Single(transactions);
        Assert.Equal(PaymentStatus.Paid, transactions[0].Status);
        Assert.NotNull(transactions[0].PaidAt);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_IsIdempotent_DuplicateEventIgnored()
    {
        await _context.HandleStripePaymentEventAsync(
            "pi_test_789", "evt_test_4", PaymentStatus.Paid, 2000, "usd");

        await _context.HandleStripePaymentEventAsync(
            "pi_test_789", "evt_test_4", PaymentStatus.Failed, 2000, "usd");

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == "pi_test_789");

        Assert.NotNull(transaction);
        Assert.Equal(PaymentStatus.Paid, transaction.Status);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_SetsFailedStatus()
    {
        await _context.HandleStripePaymentEventAsync(
            "pi_test_fail", "evt_test_5", PaymentStatus.Failed, 1500, "eur");

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == "pi_test_fail");

        Assert.NotNull(transaction);
        Assert.Equal(PaymentStatus.Failed, transaction.Status);
        Assert.Null(transaction.PaidAt);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_SetsRefundedStatus()
    {
        await _context.HandleStripePaymentEventAsync(
            "pi_test_refund", "evt_test_6", PaymentStatus.Paid, 3000, "usd");

        await _context.HandleStripePaymentEventAsync(
            "pi_test_refund", "evt_test_7", PaymentStatus.Refunded, 3000, "usd");

        var transaction = await _context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == "pi_test_refund");

        Assert.NotNull(transaction);
        Assert.Equal(PaymentStatus.Refunded, transaction.Status);
    }
}
