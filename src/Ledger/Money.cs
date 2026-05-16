namespace Ledger;

public record Money(decimal Amount, string Currency)
{
    public Money Add(Money addend2)
    {
        return new Money(Amount + addend2.Amount, Currency);
    }
}