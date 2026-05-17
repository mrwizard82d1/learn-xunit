namespace Ledger.Tests;

public class MoneyTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void InstancesWithEqualCurrencyAndAmountAreEqual()
    {
        var actual = new Money(778.72M, "CAD");
        var expect = new Money(778.72M, "CAD");
        
        Assert.Equal(expect, actual);
    }

    [Fact]
    public void InstancesWithSameAmountButDifferentCurrencyAreNotEqual()
    {
        var actual = new Money(778.72M, "CAD");
        var expect = new Money(778.72M, "CAE");
        
        Assert.NotEqual(expect, actual);
    }

    [Fact]
    public void InstancesWithDifferentAmountsButSameCurrencyAreNotEqual()
    {
        var actual = new Money(778.72M, "CAD");
        var expect = new Money(778.71M, "CAD");
        
        Assert.NotEqual(expect, actual);
    }

    [Fact]
    public void TwoMoneyInstances_Add_SumIsCorrect()
    {
        var addend1 = new Money(607.37M, "DKK");
        var addend2 = new Money(733.74M, "DKK");

        Assert.Equal(new Money(1341.11M, "DKK"), addend1.Add(addend2));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Add_ThrowsInvalidOperationException()
    {
        var addend1 = new Money(591.18M, "PHP");
        var addend2 = new Money(268.73M, "PHO");

        Assert.Throws<InvalidOperationException>(() => addend1.Add(addend2));
    }

    [Fact]
    public void TwoMoneyInstances_Subtract_DifferenceIsCorrect()
    {
        var minuend = new Money(273.03M, "BDT");
        var subtrahend = new Money(282.35M, "BDT");

        Assert.Equal(new Money(-9.32M, "BDT"), minuend.Subtract(subtrahend));
    }

    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Subtract_ThrowsInvalidOperationException()
    {
        var minuend = new Money(157.58M, "EGP");
        var subtrahend = new Money(137.06M, "EGR");

        Assert.Throws<InvalidOperationException>(() => minuend.Subtract(subtrahend));
    }
}
