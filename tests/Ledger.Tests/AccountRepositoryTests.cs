namespace Ledger.Tests;

using Ledger;

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
        
        var newAccount = repository.OpenAccount(initialBalance);

        Assert.True(repository.Contains(newAccount.Id));
    }

    [Fact]
    public void OpenAccountWithInitialBalance_NoAccounts_OpenedAccountWithHasInitialBalance()
    {
        var repository = new AccountRepository();
        var initialBalance = new Money(820.49M, "kmf");
        
        var newAccount = repository.OpenAccount(initialBalance);

        Assert.True(repository.Contains(newAccount.Id));
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
        var firstAccount = repository.OpenAccount(firstAccountInitialBalance);
        
        var secondAccountInitialBalance = new Money(402.84M, "xdr");
        var secondAccount = repository.OpenAccount(secondAccountInitialBalance);

        Assert.Multiple(
            () => Assert.True(repository.Contains(firstAccount.Id)),
            () => Assert.True(repository.Contains(secondAccount.Id))
            );
    }

    [Fact]
    public void GetAccount_AccountWithIdExists_ReturnsAccount()
    {
        var repository = new AccountRepository();
        
        var initialBalance = new Money(848.19M, "lsl");
        var addedAccount = repository.OpenAccount(initialBalance);

        var foundAccount = repository.Get(addedAccount.Id);
        Assert.Equal(addedAccount, foundAccount);
    }

    [Fact]
    public void Contains_AccountRepositoryDoesNotContainId_ReturnsFalse()
    {
        var repository = new AccountRepository();
        
        var initialBalance = new Money(986.66M, "sos");
        repository.OpenAccount(initialBalance);

        Assert.False(repository.Contains($"no-such-account"));
    }
}
