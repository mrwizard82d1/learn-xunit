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