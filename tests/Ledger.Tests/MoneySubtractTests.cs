namespace Ledger.Tests;

public class MoneySubtractTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void TwoMoneyInstances_Subtract_DifferenceIsCorrect()
    {
        var minuend = new Money(273.03M, "BDT");
        var subtrahend = new Money(282.35M, "BDT");

        Assert.Equal(new Money(-9.32M, "BDT"), minuend.Subtract(subtrahend));
    }

    [Fact]
    public void TwoMoneyInstances_Subtract_CurrencyIsSameAsCurrencyOfFirst()
    {
        var minuend = new Money(-345.21M, "EUR");
        var subtrahend = new Money(268.54M, "EUR");

        Assert.Equal(new Money(-613.75M, "EUR"), minuend.Subtract(subtrahend));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Subtract_ThrowsInvalidOperationException()
    {
        var minuend = new Money(157.58M, "EGP");
        var subtrahend = new Money(137.06M, "EGR");

        Assert.Throws<InvalidOperationException>(() => minuend.Subtract(subtrahend));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Subtract_ErrorMessageStartsWithCorrectText()
    {
        var minuend = new Money(-852.19M, "SCR");
        var subtrahend = new Money(288.67M, "CUC");

        var ex = Assert.Throws<InvalidOperationException>(() => minuend.Subtract(subtrahend));
        Assert.Matches(@"\bsubtract\b", ex.Message);
    }
}
