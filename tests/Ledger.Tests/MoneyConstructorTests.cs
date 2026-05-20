namespace Ledger.Tests;

// Candidate tests:
//
public class MoneyConstructorTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void ConstructCurrency_WithPositiveAmount_ReportsPositive()
    {
        var actual = new Money(199.45M, "BBD");

        Assert.True(actual.Amount > 0);
    }

    [Fact]
    public void ConstructCurrency_WithNegativeAmount_ReportsNegative()
    {
        var actual = new Money(-439.45M, "IDR");

        Assert.True(actual.Amount < 0);
    }

    [Fact]
    public void ConstructCurrency_WithZeroAmount_ReportsZero()
    {
        var actual = new Money(-0.00M, "TTD");

        Assert.Equal(0, actual.Amount);
    }

    [Fact]
    public void ConstructCurrency_WithMixedCaseCurrency_ReportsUpperCase()
    {
        var actual = new Money(-101.87M, "gMd");

        Assert.Equal("GMD", actual.Currency);
    }

    [Fact]
    public void ConstructCurrency_WithWhitespaceAroundCurrency_ReportsCorrectCurrency()
    {
        var actual = new Money(273.91M, " MNT\t");

        Assert.Equal("MNT", actual.Currency);
    }

    [Fact]
    public void ConstructCurrency_WithEmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(450.81M, ""));
    }
}
