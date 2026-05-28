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

    public static IEnumerable<object[]> AddCases =>
        new[]
        {
            // Order: addend1, addend2, expected sum
            new object[]
            {
                new Money(607.37M, "DKK"),
                new Money(733.74M, "DKK"),
                new Money(1341.11M, "DKK"),
            },
            new object[]
            {
                new Money(871.13M, "DKK"),
                new Money(-892.52M, "DKK"),
                new Money(-21.39M, "DKK"),
            },
            new object[]
            {
                new Money(0M, "DKK"),
                new Money(913.38M, "DKK"),
                new Money(913.38M, "DKK"),
            },
        };

    [Theory]
    [MemberData(nameof(AddCases))]
    public void Add_ProducesExpectedSum(Money addend1, Money addend2, Money expected)
    {
        Assert.Equal(expected, addend1.Add(addend2));
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
