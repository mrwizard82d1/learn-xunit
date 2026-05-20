namespace Ledger.Tests;

// Candidate tests:
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
    public void TwoMoneyInstances_Add_TypeOfReturnValueIsMoney()
    {
        var addend1 = new Money(269.84M, "TND");
        var addend2 = new Money(530.90M, "TND");

        Assert.IsType<Money>(addend1.Add(addend2));
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
    
    [Fact]
    public void TwoMoneyInstancesDifferentCurrencies_Subtract_ErrorMessageStartsWithCorrectText()
    {
        var minuend = new Money(-852.19M, "SCR");
        var subtrahend = new Money(288.67M, "CUC");
        
        var ex = Assert.Throws<InvalidOperationException>(() => minuend.Subtract(subtrahend));
        Assert.Matches(@"\bsubtract\b", ex.Message);
    }
    
    // Arithmetic invariants
    
    // Identity element
    
    [Fact]
    public void TwoMoneyInstancesButSecondZero_Add_SumEqualsFirstArgument()
    {
        var addend1 = new Money(-591.35M, "GGP");
        var addend2 = new Money(0M, "GGP");

        Assert.Equal(new Money(-591.35M, "GGP"), addend1.Add(addend2));
    }
    
    [Fact]
    public void TwoMoneyInstancesButFirstZero_Add_SumEqualsSecondArgument()
    {
        var addend1 = new Money(0M, "GGP");
        var addend2 = new Money(-591.35M, "GGP");

        Assert.Equal(new Money(-591.35M, "GGP"), addend1.Add(addend2));
    }
    
    // Commutativity
    
    [Fact]
    public void TwoMoneyInstances_Add_Commutes()
    {
        var addend1 = new Money(354.49M, "CHF");
        var addend2 = new Money(765.75M, "CHF");

        Assert.Equal(addend1.Add(addend2), addend2.Add(addend1));
    }
    
    // Associativity
    
    [Fact]
    public void ThreeMoneyInstances_Add_Associates()
    {
        var addend1 = new Money(-502.87M, "lrd");
        var addend2 = new Money(-309.46M, "Lrd");
        var addend3 = new Money(686.38M, "lrD");

        Assert.Equal(addend1.Add(addend2.Add(addend3)), (addend1.Add(addend2)).Add(addend3));
    }
    
    // Subtraction is identical to adding the negative
    
    [Fact]
    public void TwoMoneyInstances_Subtract_EqualToAdditionOfNegative()
    {
        var minuend = new Money(-316.43M, "PYG");
        var subtrahend = new Money(-835.06M, "PYG");
        var negativeSubtrahend = new Money(835.06M, "PYG");

        Assert.Equal(minuend.Subtract(subtrahend), minuend.Add(negativeSubtrahend));
    }
}
