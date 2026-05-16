namespace Ledger;

public record Money(decimal Amount, string Currency)
{
    public Money Add(Money addend2)
    {
        EnsureSameCurrency(addend2);
        return new Money(Amount + addend2.Amount, Currency);
    }

    public Money Subtract(Money subtrahend)
    {
        EnsureSameCurrency(subtrahend);
        return new Money(Amount - subtrahend.Amount, Currency);
    }

    private void EnsureSameCurrency(Money instance)
    {
        if (Currency != instance.Currency)
        {
            throw new InvalidOperationException($"Different currencies: {Currency} != {instance.Currency}");
        }
    }
}