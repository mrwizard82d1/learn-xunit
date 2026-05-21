namespace Ledger.Tests;

public class AccountRepositoryTests
{
    [Fact]
    public void SmokeTest()
    {
        Assert.Equal(4, 2+2);
    }

    [Fact]
    public void Contains()
    {
        var repository = new AccountRepository();
        const string soughtId = "";
        
        
        Assert.Throws<NotImplementedException>(() => repository.Contains(soughtId));
    }
}

public record AccountRepository
{
    public bool Contains(string candidateId) => throw new NotImplementedException();
}

