namespace Ledger.Tests;

public class AccountRepositoryTests
{
    private readonly AccountRepository _repository = new AccountRepository();

    [Fact]
    public void SmokeTest()
    {
        Assert.Equal(4, 2+2);
    }

    [Fact]
    public void OpenAccount_NoAccounts_AccountWithNewAccountIdExists()
    {
        var initialBalance = new Money(508.84M, "eur");
        
        var newAccount = _repository.OpenAccount(initialBalance);

        Assert.True(_repository.Contains(newAccount.Id));
    }

    [Fact]
    public void OpenAccountWithInitialBalance_NoAccounts_OpenedAccountHasInitialBalance()
    {
        var initialBalance = new Money(820.49M, "kmf");
        
        var newAccount = _repository.OpenAccount(initialBalance);

        Assert.Equal(new Money(820.49M, "kmf"), newAccount.Balance);
    }

    [Fact]
    public void OpenAccount_OneAccountExists_ReturnsDifferentAccounts()
    {
        var firstAccountInitialBalance = new Money(973.85M, "mdl");
        var firstAccount = _repository.OpenAccount(firstAccountInitialBalance);
        
        var secondAccountInitialBalance = new Money(825.98M, "myr");
        var secondAccount = _repository.OpenAccount(secondAccountInitialBalance);

        Assert.NotEqual(firstAccount, secondAccount);
    }

    [Fact]
    public void OpenAccount_OneAccountExists_ReturnsAccountWithDifferentId()
    {
        var firstAccountInitialBalance = new Money(768.77M, "tmt");
        var firstAccount = _repository.OpenAccount(firstAccountInitialBalance);
        
        var secondAccountInitialBalance = new Money(768.77M, "tmt");
        var secondAccount = _repository.OpenAccount(secondAccountInitialBalance);

        Assert.NotEqual(firstAccount.Id, secondAccount.Id);
    }

    [Fact]
    public void OpenAccount_OneAccountExists_TwoAccountsWithDifferentIdsExist()
    {
        var firstAccountInitialBalance = new Money(848.19M, "lsl");
        var firstAccount = _repository.OpenAccount(firstAccountInitialBalance);
        
        var secondAccountInitialBalance = new Money(402.84M, "xdr");
        var secondAccount = _repository.OpenAccount(secondAccountInitialBalance);

        Assert.Multiple(
            () => Assert.True(_repository.Contains(firstAccount.Id)),
            () => Assert.True(_repository.Contains(secondAccount.Id))
            );
    }

    [Fact]
    public void GetAccount_AccountWithIdExists_ReturnsAccount()
    {
        var initialBalance = new Money(848.19M, "lsl");
        var addedAccount = _repository.OpenAccount(initialBalance);

        var foundAccount = _repository.Get(addedAccount.Id);
        Assert.Equal(addedAccount, foundAccount);
    }

    [Fact]
    public void Contains_AccountRepositoryDoesNotContainId_ReturnsFalse()
    {
        var initialBalance = new Money(986.66M, "sos");
        _repository.OpenAccount(initialBalance);

        Assert.False(_repository.Contains("no-such-account"));
    }
}
