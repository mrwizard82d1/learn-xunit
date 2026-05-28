namespace Ledger.Tests;

public class MoneyAddTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void TwoMoneyInstances_Add_TypeOfReturnValueIsMoney()
    {
        var addend1 = new Money(269.84M, "TND");
        var addend2 = new Money(530.90M, "TND");

        Assert.IsType<Money>(addend1.Add(addend2));
    }

    [Theory]
    [InlineData(607.37, 733.74, 1341.11)] // positive sum
    [InlineData(871.13, -892.52, -21.39)] // negative sum
    [InlineData(0, 913.38, 913.38)] // first operand is zero
    public void Add_SameCurrency_ProducesExpectedSum(decimal addend1, decimal addend2, decimal expectSum)
    {
        const string currency = "DKK";
        var actualSum = new Money(addend1, currency).Add(new Money(addend2, currency));
        Assert.Equal(new Money(expectSum, currency), actualSum);
    }

    [Fact]
    public void TwoMoneyInstances_Add_CurrencyIsSameAsCurrencyOfFirst()
    {
        var addend1 = new Money(742.19M, "BWP");
        var addend2 = new Money(815.62M, "BWP");

        Assert.Equal(new Money(1557.81M, "BWP"), addend1.Add(addend2));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Add_ThrowsInvalidOperationException()
    {
        var addend1 = new Money(591.18M, "PHP");
        var addend2 = new Money(268.73M, "PHO");

        Assert.Throws<InvalidOperationException>(() => addend1.Add(addend2));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Add_ErrorMessageContainsCorrectCurrencyCodes()
    {
        var addend1 = new Money(391.76M, "RUB");
        var addend2 = new Money(834.68M, "NIO");

        var ex = Assert.Throws<InvalidOperationException>(() => addend1.Add(addend2));

        Assert.Multiple(
            () => Assert.Contains("RUB", ex.Message),
            () => Assert.Contains("NIO", ex.Message));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Add_ErrorMessageContainsOperation()
    {
        var addend1 = new Money(-282.55M, "QAR");
        var addend2 = new Money(995.59M, "BOB");

        var ex = Assert.Throws<InvalidOperationException>(() => addend1.Add(addend2));
        Assert.Matches(@"\badd\b", ex.Message);
    }
}
