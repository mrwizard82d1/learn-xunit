namespace Ledger.Tests;

// Candidate tests:
// 
// - Constructor normalizes currency to uppercase
// - Currency must be non-empty
// - Same instances
// - Instances have correct currency code after construction (all uppercase)
// - Correctly `Add` a `Money` instance with negative amount
// - `Add` can return a `Money` instance with a negative amount
// - `Add` returns a value whose `Currency` property is equal to the `Currency` property of the first argument
// - `Subtract` returns a value whose `Currency` property is equal to the `Currency` property of the first argument
// - Exception message contains the offending currency code
// - Exception message text starts with
//   - "Cannot add"
//   - "Cannot subtract"
// - Add returns an instance of `Money`
// - Add a "zero" amount of Money returns the other argument
// - Addition is commutative
// - Addition is associative
// - Subtraction is equivalent to adding a negative
//
public class MoneyTests
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
