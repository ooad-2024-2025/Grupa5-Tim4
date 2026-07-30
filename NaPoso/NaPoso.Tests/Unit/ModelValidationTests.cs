using System.ComponentModel.DataAnnotations;
using NaPoso.Models;
using static NaPoso.Enums.Enums;

namespace NaPoso.Tests.Unit;

public class ModelValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Oglas_Naslov_CanBeSetToMaxLength()
    {
        var oglas = new Oglas
        {
            Naslov = new string('A', 100),
            Opis = "Test opis",
            Lokacija = "Sarajevo",
            TipPosla = "IT",
            CijenaPosla = 100
        };

        var results = Validate(oglas);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(Oglas.Naslov)));
    }

    [Fact]
    public void Oglas_Naslov_CanBeNull()
    {
        // Naslov is string? (nullable reference type), so assignment of null compiles fine.
        // The [Required] attribute means validation will report an error for null.
        Oglas? oglas = new Oglas();
        oglas.Naslov = null;
        Assert.Null(oglas.Naslov);
    }

    [Fact]
    public void Recenzija_Ocjena_CanBeSetToRange()
    {
        var recenzija1 = new Recenzija { Ocjena = 1, Sadrzaj = "Test", KlijentId = "k1", RadnikId = "r1" };
        var recenzija3 = new Recenzija { Ocjena = 3, Sadrzaj = "Test", KlijentId = "k1", RadnikId = "r1" };
        var recenzija5 = new Recenzija { Ocjena = 5, Sadrzaj = "Test", KlijentId = "k1", RadnikId = "r1" };

        Assert.Empty(Validate(recenzija1));
        Assert.Empty(Validate(recenzija3));
        Assert.Empty(Validate(recenzija5));
    }

    [Fact]
    public void PaymentTransaction_Amount_CanBeZero()
    {
        var pt = new PaymentTransaction
        {
            UserId = "user1",
            StripePaymentIntentId = "pi_test",
            StripeEventId = "evt_test",
            Amount = 0
        };

        var results = Validate(pt);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(PaymentTransaction.Amount)));
    }

    [Fact]
    public void PaymentTransaction_Currency_DefaultIsUsd()
    {
        var pt = new PaymentTransaction();

        Assert.Equal("usd", pt.Currency);
    }

    [Fact]
    public void Korisnik_Verified_DefaultIsFalse()
    {
        var korisnik = new Korisnik();

        Assert.False(korisnik.Verified);
    }

    [Fact]
    public void Chat_CreatedAt_IsUtcByDefault()
    {
        var chat = new Chat();

        Assert.Equal(DateTimeKind.Utc, chat.CreatedAt.Kind);
    }

    [Fact]
    public void Poruka_PoslanoAt_IsUtcByDefault()
    {
        var poruka = new Poruka();

        Assert.Equal(DateTimeKind.Utc, poruka.PoslanoAt.Kind);
    }
}
