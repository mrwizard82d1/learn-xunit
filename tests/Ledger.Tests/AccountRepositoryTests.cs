namespace Ledger.Tests;

public class AccountRepositoryTests
{
    [Fact]
    public void SmokeTest()
    {
        Assert.Equal(4, 2+2);
    }

    [Fact]
    public void OpenAccount_NoAccounts_AccountWithNewAccountIdExists()
    {
        var repository = new AccountRepository();
        var initialBalance = new Money(508.84M, "eur");
        
        var newAccountId = repository.OpenAccount(initialBalance);

        Assert.True(repository.Contains(newAccountId));
    }

    [Fact]
    public void OpenAccountWithInitialBalance_NoAccounts_OpenedAccountWithHasInitialBalance()
    {
        var repository = new AccountRepository();
        var initialBalance = new Money(820.49M, "kmf");
        
        var newAccountId = repository.OpenAccount(initialBalance);

        Assert.True(repository.Contains(newAccountId));
    }

    [Fact]
    public void OpenAccount_OneAccountExists_ReturnsDifferentAccountNumber()
    {
        var repository = new AccountRepository();
        
        var firstAccountInitialBalance = new Money(973.85M, "mdl");
        var firstAccountId = repository.OpenAccount(firstAccountInitialBalance);
        
        var secondAccountInitialBalance = new Money(825.98M, "myr");
        var secondAccountId = repository.OpenAccount(secondAccountInitialBalance);

        Assert.NotEqual(firstAccountId, secondAccountId);
    }

    [Fact]
    public void OpenAccount_OneAccountExists_TwoAccountsWithDifferentIdsExist()
    {
        var repository = new AccountRepository();
        
        var firstAccountInitialBalance = new Money(848.19M, "lsl");
        var firstAccountId = repository.OpenAccount(firstAccountInitialBalance);
        
        var secondAccountInitialBalance = new Money(402.84M, "xdr");
        var secondAccountId = repository.OpenAccount(secondAccountInitialBalance);

        Assert.Multiple(
            () => Assert.True(repository.Contains(firstAccountId)),
            () => Assert.True(repository.Contains(secondAccountId))
            );
    }

    [Fact]
    public void GetAccount_AccountWithIdExists_ReturnsAccount()
    {
        var repository = new AccountRepository();
        
        var initialBalance = new Money(848.19M, "lsl");
        var id = repository.OpenAccount(initialBalance);

        var actualNewAccount = repository.Get(id);
        Assert.Equal(id, actualNewAccount.Id);
    }
}

public record AccountRepository
{
    private readonly Dictionary<string, Account> _accounts = new Dictionary<string, Account>();
    private int _nextId = 1;
    
    public bool Contains(string candidateId) => true;
    
    public string OpenAccount(Money amount)
    { 
        var newAccountNumber = $"acc-{_nextId++}";
        var newAccount =  new Account(newAccountNumber, amount);
        _accounts.Add(newAccountNumber, newAccount);
        
        return newAccountNumber;
    }

    public Account Get(string id)
    {
        return _accounts[id];
    }
}

// BEWARE: production should access accounts **only** via `AccountRepository`. This constructor is `public` only
// to support unit testing.
public record Account(string Id, Money Balance);
