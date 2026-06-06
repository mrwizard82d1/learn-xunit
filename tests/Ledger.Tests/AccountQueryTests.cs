namespace Ledger.Tests;

public class AccountQueryTests : IClassFixture<SeededAccountsFixture>
{
    private readonly SeededAccountsFixture _fixture;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AccountQueryTests(SeededAccountsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void Get_KnownAccountId_ReturnsAccount()
    {
        var fetched = _fixture.Repository.Get(_fixture.Checking.Id);
        
        Assert.Equal(_fixture.Checking, fetched);
    }

    [Fact]
    public void Contains_KnownAccountId_ReturnsTrue()
    {
        Assert.True(_fixture.Repository.Contains(_fixture.Savings.Id));
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
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