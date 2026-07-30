using NaPoso.Models;
using NaPoso.Enums;
using static NaPoso.Enums.Enums;

namespace NaPoso.Tests;

public class ModelTests
{
    [Fact]
    public void PaymentTransaction_DefaultStatus_IsPending()
    {
        var transaction = new PaymentTransaction();
        Assert.Equal(PaymentStatus.Pending, transaction.Status);
    }

    [Fact]
    public void PaymentTransaction_DefaultCurrency_IsUsd()
    {
        var transaction = new PaymentTransaction();
        Assert.Equal("usd", transaction.Currency);
    }

    [Fact]
    public void Statistika_DefaultValues_AreZero()
    {
        var stats = new Statistika();
        Assert.Equal(0, stats.BrojKorisnika);
        Assert.Equal(0, stats.BrojPoslova);
        Assert.Equal(0, stats.BrojKlijenata);
        Assert.Equal(0, stats.BrojRadnika);
        Assert.Equal(0, stats.BrojZavrsenihPoslova);
        Assert.Equal(0, stats.AktivniPoslovi);
        Assert.Equal(0, stats.PlaceniPoslovi);
        Assert.Equal(0, stats.ProsjecnaOcjena);
    }

    [Fact]
    public void Oglas_DefaultStatus_IsNeaktivan()
    {
        var oglas = new Oglas();
        Assert.Equal(Status.Neaktivan, oglas.Status);
    }

    [Fact]
    public void PaymentStatus_Enum_ContainsAllValues()
    {
        Assert.True(Enum.IsDefined(typeof(PaymentStatus), PaymentStatus.Pending));
        Assert.True(Enum.IsDefined(typeof(PaymentStatus), PaymentStatus.Paid));
        Assert.True(Enum.IsDefined(typeof(PaymentStatus), PaymentStatus.Failed));
        Assert.True(Enum.IsDefined(typeof(PaymentStatus), PaymentStatus.Refunded));
    }

    [Fact]
    public void Status_Enum_ContainsExpectedValues()
    {
        Assert.Equal(0, (int)Status.Neaktivan);
        Assert.Equal(1, (int)Status.Aktivan);
        Assert.Equal(2, (int)Status.Prihvacen);
        Assert.Equal(4, (int)Status.Placen);
        Assert.Equal(5, (int)Status.Zavrsen);
    }
}
