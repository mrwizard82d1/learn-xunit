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
        var actual = new Money(778.72M, "CAD");
        var expect = new Money(778.72M, "CAD");
        
        Assert.Equal(expect, actual);
    }
}
