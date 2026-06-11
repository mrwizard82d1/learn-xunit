namespace Ledger.Tests;

public class AccountScenarioTests
{
    private readonly ITestOutputHelper _output;

    public AccountScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);
}