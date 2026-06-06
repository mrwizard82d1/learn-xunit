namespace Ledger.Tests;

public class AccountQueryTests
{
    [Fact]
    public void SmokeTest()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void Get_KnownAccountId_ReturnsAccount()
    {
        var repository = new AccountRepository();
        var opened = repository.OpenAccount(new Money(704.19M, new CurrencyCode("COP")));

        var fetched = repository.Get(opened.Id);
        
        Assert.Equal(opened, fetched);
    }

    [Fact]
    public void Contains_KnownAccountId_ReturnsTrue()
    {
        var repository = new AccountRepository();
        var opened = repository.OpenAccount(new Money(776.94M,  new CurrencyCode("NGN")));
        
        Assert.True(repository.Contains(opened.Id));
    }
}

public sealed class SeededAccountsFixture
{
    // The `Repository` member is public. This choice is more pedagogical than
    // required. One could probably encapsulate `Repository` and complete the
    // tutorial with minor modifications. I've chosen to leave it `public`
    // simply to move on with the goal: learning `xUnit`. This comment is a 
    // reminder to "future me" that it may not be the most robust
    // implementation.
    // ReSharper disable once MemberCanBePrivate.Global
    public AccountRepository Repository { get; }
    public Account Checking { get; }
    public Account Savings { get; }
    public Account Empty { get; }

    public SeededAccountsFixture()
    {
        Repository = new AccountRepository();
        Checking = Repository.OpenAccount(new Money(270.95M, new CurrencyCode("IRR")));
        Savings = Repository.OpenAccount(new Money(336.20M, new CurrencyCode("IRR")));
        Empty = Repository.OpenAccount(new Money(0M, new CurrencyCode("IRR")));
    }
}