namespace Ledger;

public record Money(decimal Amount, string Currency)
{
    public Money Add(Money addend2)
    {
        if (Currency != addend2.Currency)
        {
            throw new InvalidOperationException($"Different currencies: {Currency} != {addend2.Currency}");
        }
        
        return new Money(Amount + addend2.Amount, Currency);
    }

    public Money Subtract(Money subtrahend)
    {
        if (Currency != subtrahend.Currency)
        {
            throw new InvalidOperationException($"Different currencies: {Currency} != {subtrahend.Currency}");
        }
        
        return new Money(Amount - subtrahend.Amount, Currency);
    }
}