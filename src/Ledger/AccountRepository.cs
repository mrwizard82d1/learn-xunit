namespace Ledger;

public class AccountRepository
{
    private readonly Dictionary<string, Account> _accounts = new Dictionary<string, Account>();
    private int _nextId = 1;
    
    public bool Contains(string candidateId) => _accounts.ContainsKey(candidateId);
    
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