# Phase 2 — Lifecycle: constructor, `IDisposable`, `IAsyncLifetime` (TDD)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Internalize xUnit's per-test instance lifecycle — the single biggest cultural difference from NUnit. By the end:

- **Constructor = `[SetUp]`. `IDisposable.Dispose` = `[TearDown]`.** xUnit reuses the language's existing constructs instead of inventing new attributes.
- **Each `[Fact]` runs on a fresh instance of the test class.** Field initializers run per test. No `[SetUp]` attribute needed; the constructor IS setup.
- **`IAsyncLifetime`** for setup/teardown that must `await`.

Vehicle: an in-memory `AccountRepository` that tests need a clean copy of each time.

## Decisions made

- (inherits Phase 1's smoke-as-canary, inline-then-extract, and skeleton+test patterns)
- **Domain language: accounts are *opened* and *closed*, not *added* and *removed*.** `AccountRepository.OpenAccount(initialBalance)` issues a new account ID and returns the resulting `Account`. Callers cannot supply an ID — the repository owns ID generation. `Account`'s constructor remains public for testability, but in production code the repository is the sole source of new accounts. This separates "store" (a generic CRUD concern) from "create-and-store" (the domain operation).
- *(add others as we go)*

---

## Steps

### Step 1 — Per-class smoke for `AccountRepositoryTests`  `[x]`

Create `tests/Ledger.Tests/AccountRepositoryTests.cs` with the per-class smoke `[Fact]` (`Assert.Equal(4, 2 + 2)` per established pattern). Run, see it pass.

### Step 2 — RED → GREEN: new repository contains no accounts  `[x]`

Test: a freshly constructed `AccountRepository` returns `false` for any `Contains(id)` lookup.

Inline skeleton in `AccountRepositoryTests.cs`:

```csharp
public class AccountRepository
{
    public bool Contains(string id) => throw new NotImplementedException();
}
```

Red on `NotImplementedException`. Make green minimally — return `false`. The next test will force the real shape.

### Step 3 — RED → GREEN: opening an account stores it in the repository (introduces `Account`)  `[x]`

Test: after calling `_repo.OpenAccount(balance)`, the returned account's ID is known to the repository.

You need an `Account` type to write this. Inline next to the repository, with a marker comment recording the design intent (the compiler can't enforce it, so make it visible to humans):

```csharp
// Production code obtains accounts via AccountRepository.OpenAccount.
// This public constructor exists for test construction only.
public record Account(string Id, Money Balance);
```

Minimal — an `Id` and the `Money` balance from Phase 1. Will grow in later phases (account type, overdraft limit, etc.).

Repository skeleton for the new method:

```csharp
public Account OpenAccount(Money initialBalance) => throw new NotImplementedException();
```

Red on `NotImplementedException`. Implement minimally — generate a sequential ID, store the account, return it. A `Dictionary<string, Account>` works because the next test you'll want is "look up an account by ID":

```csharp
private readonly Dictionary<string, Account> _accounts = new();
private int _nextId = 1;

public Account OpenAccount(Money initialBalance)
{
    var account = new Account($"acc-{_nextId++}", initialBalance);
    _accounts[account.Id] = account;
    return account;
}

public bool Contains(string accountId) => _accounts.ContainsKey(accountId);
```

Green.

**Candidate tests worth adding to your list** (each a natural follow-on red-green cycle, not required for Phase 2's lifecycle focus):

- `OpeningAccount_ReturnsAccountWithInitialBalance` — `_repo.OpenAccount(balance).Balance` equals `balance`.
- `OpeningMultipleAccounts_AssignsDistinctIds` — two opens yield two different IDs.
- A `Get(id)` method that returns the account or signals absence.
- A `Close(id)` method for completeness (the inverse of `Open`).

### Step 4 — REFACTOR: extract `Account` and `AccountRepository` to production files  `[ ]`

Per the inline-then-extract decision from Phase 1:
- `src/Ledger/Account.cs`
- `src/Ledger/AccountRepository.cs`

Add `using Ledger;` to `AccountRepositoryTests.cs` (or rely on existing implicit usings). Re-run; should still be green — pure structural refactor.

### Step 5 — REFACTOR: hoist construction to a constructor (the `[SetUp]` equivalent)  `[ ]`

Right now each test starts with `var repo = new AccountRepository();`. Hoist it to a field initialized at class construction:

```csharp
public class AccountRepositoryTests
{
    private readonly AccountRepository _repo = new();

    [Fact]
    public void SmokeTests() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void NewRepository_ContainsNoAccounts()
        => Assert.False(_repo.Contains("any-id"));

    // ... other tests using _repo ...
}
```

The field initializer runs as part of the constructor — same lifecycle, less ceremony than declaring the field separately and initializing it in an explicit constructor body. Reach for an explicit constructor only when setup needs more than one expression.

**Why this is xUnit's `[SetUp]` equivalent:** xUnit creates a **fresh instance of the test class for every `[Fact]`**. The constructor runs per test, which means field initializers run per test, which means `_repo` is a fresh `AccountRepository` for every test. No `[SetUp]` attribute needed; the language gives you the lifecycle hook for free.

Re-run; all tests still green.

### Step 6 — DEMO: prove per-test isolation (optional but cheap)  `[ ]`

Two tests that *would* fail if instances were shared. They're documentation that the per-test invariant is being relied on:

```csharp
[Fact]
public void Lifecycle_PartOne_OpensAccount()
{
    var account = _repo.OpenAccount(new Money(100M, "USD"));
    Assert.True(_repo.Contains(account.Id));
}

[Fact]
public void Lifecycle_PartTwo_RepoStartsFresh()
{
    // If PartOne and PartTwo shared the same instance, the ID counter would
    // have advanced past "acc-1". Getting "acc-1" back proves each [Fact] got
    // its own fresh AccountRepositoryTests instance.
    var account = _repo.OpenAccount(new Money(50M, "USD"));
    Assert.Equal("acc-1", account.Id);
}
```

If xUnit ran `PartOne` first against a shared instance, `PartTwo`'s newly-opened account would get `"acc-2"` (the counter had already advanced). The fact that **both pass regardless of order** proves each test got its own fresh `AccountRepositoryTests` instance with its own fresh `_repo` (and its own fresh counter).

Keep these as documentation, or delete them now that you've internalized the point. Your call.

### Step 7 — TEARDOWN: `IDisposable.Dispose`  `[ ]`

For test classes that hold cleanup-requiring resources (file handles, db connections, temp dirs, environment-variable mutations), implement `IDisposable`:

```csharp
public class AccountRepositoryTests : IDisposable
{
    private readonly AccountRepository _repo = new();

    // ... tests ...

    public void Dispose()
    {
        // Runs once per test, after the test method.
    }
}
```

For an in-memory repository there's nothing to clean up, so this is the pattern, not a useful action. Where it actually earns its keep:

- File-based tests creating temp files → delete them in `Dispose`.
- Database tests opening connections → close them in `Dispose`.
- Tests that mutate `Environment.SetEnvironmentVariable(...)` → restore in `Dispose`.

**NUnit ↔ xUnit:** `[TearDown]` → `Dispose`. Same per-test lifecycle as the constructor. xUnit chose the language's existing cleanup contract instead of inventing a new attribute. If you also want one-time class-level teardown, that's `IClassFixture<T>` territory (Phase 4).

### Step 8 — `IAsyncLifetime` (preview, not yet used)  `[ ]`

For setup or teardown that must `await` something:

```csharp
public class SomeAsyncTests : IAsyncLifetime
{
    public async Task InitializeAsync() { /* await setup */ }
    public async Task DisposeAsync()    { /* await teardown */ }
}
```

You can't `await` in a constructor (C# language constraint), and you shouldn't block on a `.Result` or `.Wait()` in tests. `IAsyncLifetime` is the escape hatch.

Not needed yet — our in-memory repository is synchronous. We'll wire it in for real when async persistence enters in Phase 6.

**NUnit ↔ xUnit:** NUnit's `[SetUp]`/`[OneTimeSetUp]` can be `async`. xUnit splits this into the language-native `IDisposable`/`IAsyncDisposable` for teardown plus `IAsyncLifetime` for async setup — slightly more interfaces, but each does exactly one thing.

### Step 9 — NUnit↔xUnit lifecycle reflection  `[ ]`

In **Notes & questions** below, capture:

- Which NUnit lifecycle attributes you used most often, and where their xUnit equivalent lives.
- Whether the per-test instance model feels natural to you, or whether the lack of an explicit `[SetUp]` attribute initially read as "where's the setup?" (common stumble even for veterans).
- Any real bugs you've had in NUnit/JUnit/pytest suites caused by shared mutable state between tests — exactly the class of bug xUnit's design prevents by construction.

---

## Stretch (optional)

- Read **Jim Newkirk's** blog posts on why xUnit exists. Jim co-wrote NUnit before co-creating xUnit; his "why we started over" essays are some of the more articulate "lessons learned from building a test framework" writing in the .NET ecosystem. Search for "Jim Newkirk xUnit history" or similar.
- Add a static counter to verify the constructor really is called per test. (Hint: increment a `static int` field in the test-class constructor, assert in each test that the counter incremented as expected. Mostly useful as a one-time confirmation experiment.)
- Look at how xUnit handles **shared expensive setup** when per-test instances would be wasteful (e.g. spinning up a database). That's `IClassFixture<T>` — the natural follow-on, covered in Phase 4.

---

## Notes & questions

_Fill in as you go._

-
