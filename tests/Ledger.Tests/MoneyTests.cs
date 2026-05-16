namespace Ledger.Tests;

public class MoneyTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void TwoMoneyInstancesWithEqualCurrencyAndAmountAreEqual()
    {
        var left = new Money(3.5M, "USD");
        var right = new Money(3.5M, "USD");
        
        Assert.Equal(left, right);
    }
}

public class Money
{
    private decimal _amount;
    private string _currency;

    public Money(decimal amount, string currency)
    {
        _amount = 0;
        _currency = String.Empty;
    }
}