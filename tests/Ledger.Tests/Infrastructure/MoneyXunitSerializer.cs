using Ledger;
using Ledger.Tests.Infrastructure;
[assembly: Xunit.Sdk.RegisterXunitSerializer(typeof(MoneyXunitSerializer), typeof(Money))]

namespace Ledger.Tests.Infrastructure;

using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Xunit.Sdk;

public class MoneyXunitSerializer : IXunitSerializer
{
    public bool IsSerializable(Type type, object? value, [NotNullWhen(false)] out string? failureReason)
    {
        if (value is null)
        {
            failureReason = "Value to serialize cannot be `null`.";
            return false;
        }
            
        if (type != typeof(Money))
        {
            failureReason = $"Type {type.Name} not supported. Only type `Money` is supported.";
            return false;
        }
            
        if (value.GetType() != typeof(Money))
        {
            failureReason = $"Cannot serialize instances of {value.GetType().Name}, " +
                            "but only instances of type, `Money`";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public string Serialize(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var actualObject = (Money)value;
        return $"{actualObject.Amount.ToString(CultureInfo.InvariantCulture)}|{actualObject.Currency}";
    }
    public object Deserialize(Type type, string serializedValue)
    {
        if (!(type == typeof(Money)))
        {
            throw new ArgumentException($"Cannot deserialize to {type.Name}. Only `Money` is supported.");
        }
        
        var components = serializedValue.Split('|');
        var amount = decimal.Parse(components[0], CultureInfo.InvariantCulture);
        var currency = components[1];
        return new Money(amount, currency);
    }
}
