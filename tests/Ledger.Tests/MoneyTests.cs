namespace Ledger.Tests;

// Candidate tests:
// 
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
    public void AnInstanceAndAnAlias_ReportTheSame()
    {
        var actual = new Money(-115.63M, "IRR");
        // ReSharper disable once InlineTemporaryVariable
        var alias = actual;
        
        Assert.Same(alias, actual);
    }

    [Fact]
    public void TwoEqualInstances_DoNotReportTheSame()
    {
        var someMoney = new Money(-975.29M, "MYR");
        var equalMoney = new Money(-975.29M, "MYR");
        
        Assert.NotSame(someMoney, equalMoney);
    }

    [Fact]
    public void TwoMoneyInstances_Add_SumIsCorrect()
    {
        var addend1 = new Money(607.37M, "DKK");
        var addend2 = new Money(733.74M, "DKK");

        Assert.Equal(new Money(1341.11M, "DKK"), addend1.Add(addend2));
    }

    [Fact]
    public void TwoMoneyInstancesButOneNegative_Add_SumIsCorrect()
    {
        var addend1 = new Money(871.13M, "DKK");
        var addend2 = new Money(-892.52M, "DKK");

        Assert.Equal(new Money(-21.39M, "DKK"), addend1.Add(addend2));
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
