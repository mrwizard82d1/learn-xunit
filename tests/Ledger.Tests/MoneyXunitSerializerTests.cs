using Ledger.Tests.Infrastructure;

namespace Ledger.Tests;

public class MoneyXunitSerializerTests
{
    private readonly MoneyXunitSerializer _serializer = new();

    [Theory]
    [InlineData(757.55, "COP")]
    [InlineData(-578.19, "SGD")]
    [InlineData(0, "GGP")]
    public void SerializeDeserialize_AnyMoney_EqualsOriginalMoney(decimal amount, string currency)
    {
        var expectedMoney =  new Money(amount, new CurrencyCode(currency));
        
        var actualMoney = 
            _serializer.Deserialize(typeof(Money), _serializer.Serialize(expectedMoney));
        Assert.Equal(expectedMoney, actualMoney);
    }

    [Fact]
    public void IsSerializable_MoneyTypeAndMoneyValue_ReturnsTrue()
    {
        var someMoney = new Money(941.31M, new CurrencyCode("BTN"));
        var isSerializable = _serializer.IsSerializable(someMoney.GetType(), someMoney, out var failureReason);
        Assert.Multiple(
            () => Assert.True(isSerializable),
            () => Assert.Equal("", failureReason));
    }
    
    [Fact]
    public void IsSerializable_NullValue_ReturnsFalseWithNullMessage()
    {
        var isSerializable = _serializer.IsSerializable(typeof(Money), null, out var failureReason);
        Assert.Multiple(
            () => Assert.False(isSerializable),
            () => Assert.Contains("null", failureReason, StringComparison.OrdinalIgnoreCase)
        );
    }
    
    [Fact]
    public void IsSerializable_NonMoneyType_ReturnsFalseWithTypeMessage()
    {
        const decimal someDecimal = 225.18M; 
        var isSerializable = _serializer.IsSerializable(someDecimal.GetType(), someDecimal, out var failureReason);
        Assert.Multiple(
            () => Assert.False(isSerializable),
            () => Assert.StartsWith("Type decimal not supported", failureReason, StringComparison.OrdinalIgnoreCase));
    }
    
    [Fact]
    public void IsSerializable_MoneyTypeButNonMoneyValue_ReturnsFalseWithValueTypeMessage()
    {
        const decimal someDecimal = 225.18M; 
        var isSerializable = _serializer.IsSerializable(typeof(Money), someDecimal, out var failureReason);
        Assert.Multiple(
            () => Assert.False(isSerializable),
            () => Assert.StartsWith("Cannot serialize instances of decimal", 
                                    failureReason, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Deserialize_NonMoneyType_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() => _serializer.Deserialize(typeof(decimal), 
                                                      "100|USD"));
        Assert.Contains("decimal", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Deserialize_WithMalformedCurrency_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => _serializer.Deserialize(typeof(Money), "100|ER"));
        Assert.Contains("3 characters", ex.Message); // matches CurrencyCode's "must be exactly 3 characters"
    }
}
