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

    [Fact]
    public void TwoMoneyInstances_Add_SumIsCorrect()
    {
        var addend1 = new Money(607.37M, "DKK");
        var addend2 = new Money(734.74M, "DKK");

        Assert.Equal(new Money(1341.11M, "DKK"), addend1.Add(addend2));
    }
}
