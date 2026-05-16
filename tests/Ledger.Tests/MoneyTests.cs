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
        var left = new Money(778.72M, "CAD");
        var right = new Money(778.72M, "CAD");
        
        Assert.Equal(left, right);
    }
}

public record Money(decimal Amount, string Currency);