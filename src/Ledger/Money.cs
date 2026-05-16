namespace Ledger;

public record Money(decimal Amount, string Currency)
{
    public Money Add(Money addend2)
    {
        throw new NotImplementedException();
    }
}