namespace Ledger;

public record Money(decimal Amount, string Currency)
{
    public Money Add(Money addend2)
    {
        return new Money(0, String.Empty);
    }
}