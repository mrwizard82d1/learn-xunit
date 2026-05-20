namespace Ledger.Tests;

public class MoneyNegateTests
{
    [Fact]
    public void SmokeTests()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void PositiveMoneyInstance_Negate_EqualToOriginalInstanceWithNegativeAmount()
    {
        var value = new Money(940.95M, "MNT");

        Assert.Equal(new Money(-940.95M, "MNT"), value.Negate());
    }

    [Fact]
    public void NegativeMoneyInstance_Negate_EqualToOriginalInstanceWithPositiveAmount()
    {
        var value = new Money(-859.95M, "MNT");

        Assert.Equal(new Money(859.95M, "MNT"), value.Negate());
    }

    [Fact]
    public void ZeroMoneyInstance_Negate_EqualToZeroMoneyInstance()
    {
        var value = new Money(0M, "GMD");

        Assert.Equal(new Money(0.00M, "GMD"), value.Negate());
    }

    [Fact]
    public void MoneyInstance_Negate_PreservesCurrency()
    {
        var value = new Money(183.49M, "SBD");

        Assert.Equal("SBD", value.Negate().Currency);
    }

    [Fact]
    public void MoneyInstance_Negate_Negate_EqualsOriginal()
    {
        var value = new Money(224.62M, "SAR");

        Assert.Equal(new Money(224.62M, "SAR"), value.Negate().Negate());
    }
}
