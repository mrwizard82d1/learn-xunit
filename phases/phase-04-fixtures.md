# Phase 4 — `IClassFixture<T>` and `ICollectionFixture<T>`

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Share setup that should be built **once per class** (or **once per collection of classes**) instead of once per test. By the end:

- **`IClassFixture<T>`** for "one instance shared across all tests in this class."
- **`ICollectionFixture<T>` + `[CollectionDefinition]`** for "one instance shared across all tests in multiple classes."
- Understand why xUnit's per-test constructor lifecycle (Phase 2) is the *default* and fixtures are the *opt-in* for expensive setup — opposite of NUnit's `[SetUp]` / `[OneTimeSetUp]` weighting.
- Recognize when a fixture is the right tool vs. when constructor-per-test is fine.

Vehicle: a `SeededAccountsFixture` that pre-creates a small "standard cast" of accounts in an `AccountRepository` — the kind of setup many query-style tests want to share.

This phase relaxes strict red-green-refactor a notch. The lessons are about xUnit mechanics (lifecycle, injection, sharing) more than driving new design. You'll still smoke-test new classes and verify behavior, but the rhythm is "demonstrate the pattern" rather than "drive the next contract."

## Decisions made

- (inherits Phase 1-3 patterns: smoke as canary, inline-then-extract, CurrencyCode for currency, etc.)
- *(add others as we go)*

---

## Steps

### Step 1 — Per-class smoke for `AccountQueryTests`  `[x]`

Create `tests/Ledger.Tests/AccountQueryTests.cs` with the standard smoke `[Fact]` per the canary pattern:

```csharp
namespace Ledger.Tests;

public class AccountQueryTests
{
    [Fact]
    public void SmokeTest()
    {
        Assert.Equal(4, 2 + 2);
    }
}
```

Run. One green. This is the new test class; we'll grow it through the phase as the consumer of the fixture we're about to build.

### Step 2 — Write tests that need pre-seeded state (without a fixture yet)  `[x]`

Imagine you're writing tests for `AccountRepository.Get` and `AccountRepository.Contains` against a repository that already has accounts in it. The natural first instinct (from constructor-only-setup discipline) is to seed inside each test:

```csharp
[Fact]
public void Get_KnownAccountId_ReturnsAccount()
{
    var repository = new AccountRepository();
    var opened = repository.OpenAccount(new Money(1000M, new CurrencyCode("USD")));

    var fetched = repository.Get(opened.Id);

    Assert.Equal(opened, fetched);
}

[Fact]
public void Contains_KnownAccountId_ReturnsTrue()
{
    var repository = new AccountRepository();
    var opened = repository.OpenAccount(new Money(1000M, new CurrencyCode("USD")));

    Assert.True(repository.Contains(opened.Id));
}
```

Run. Both green. Notice the duplication: every test re-builds the repository and opens an account. For two tests this is fine. For ten tests querying against a "standard cast" of five accounts, this becomes noise that obscures what each test is actually verifying.

This is the moment a fixture earns its keep. A fixture is xUnit's answer to "I want this setup to happen once and be reused, not redone per test."

### Step 3 — Build `SeededAccountsFixture` (still inline)  `[x]`

Per the inline-then-extract pattern, sketch the fixture inside `AccountQueryTests.cs` before extracting:

```csharp
public class SeededAccountsFixture
{
    public AccountRepository Repository { get; }
    public Account Checking { get; }
    public Account Savings { get; }
    public Account Empty { get; }

    public SeededAccountsFixture()
    {
        Repository = new AccountRepository();
        Checking = Repository.OpenAccount(new Money(1000M, new CurrencyCode("USD")));
        Savings = Repository.OpenAccount(new Money(5000M, new CurrencyCode("USD")));
        Empty = Repository.OpenAccount(new Money(0M, new CurrencyCode("USD")));
    }
}
```

A few principles to know now (they'll matter at scale):

- **A fixture is just a class.** No attribute, no base class, no interface required for the simplest case. xUnit's only contract is "has a public parameterless constructor."
- **The constructor is where the work happens.** xUnit calls it once and reuses the instance.
- **Expose what tests need as public properties.** Tests reach into the fixture through these — no magic, no service location, just object access.
- **Fixtures should be read-only by the time tests see them.** Tests should *consume* the fixture's state, not mutate it. (More on this in Step 5.)

### Step 4 — Wire `IClassFixture<T>` into the test class  `[ ]`

xUnit injects the fixture into the test class via constructor — same pattern you'd expect from a DI container, but built into the framework. Update `AccountQueryTests` to receive it:

```csharp
public class AccountQueryTests : IClassFixture<SeededAccountsFixture>
{
    private readonly SeededAccountsFixture _fixture;

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
```

What's happening under the hood:

1. xUnit sees `IClassFixture<SeededAccountsFixture>` on the class.
2. Before running any test in `AccountQueryTests`, xUnit constructs *one* `SeededAccountsFixture` instance.
3. For each `[Fact]`, xUnit constructs a *new* `AccountQueryTests` instance (Phase 2's per-test lifecycle is unchanged), passing the *same* `SeededAccountsFixture` instance to its constructor.
4. After all tests in `AccountQueryTests` finish, if `SeededAccountsFixture` implements `IDisposable`, xUnit calls `Dispose`.

Run. All three tests still green. The change is invisible to the test runner; the win is in the source: the per-test seeding is gone.

**NUnit ↔ xUnit:** `[OneTimeSetUp]` (per-class) → `IClassFixture<T>` constructor. `[OneTimeTearDown]` → `IClassFixture<T>` implementing `IDisposable`. xUnit's choice of *constructor injection* over a sentinel method is the same philosophical move it made with `[SetUp]` → constructor: use the language's existing tools rather than inventing attributes.

### Step 5 — Prove the lifecycle: one fixture, many test instances  `[ ]`

Phase 2 had a similar demonstration for per-test isolation. The fixture version flips the lesson: the fixture is *shared*, and proving it requires showing the *same* instance is observed across tests.

Add an instance-counter to the fixture:

```csharp
public class SeededAccountsFixture
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    // ... rest as before
}
```

Add two tests that read the InstanceId. If they observe the same value, they got the same instance:

```csharp
[Fact]
public void Lifecycle_PartOne_RecordsFixtureInstanceId()
{
    // No assertion — this test exists to demonstrate the fixture is shared with PartTwo.
    Assert.NotEqual(Guid.Empty, _fixture.InstanceId);
}

[Fact]
public void Lifecycle_PartTwo_SeesSameFixtureInstance()
{
    // This is meaningful in combination with PartOne: both tests received the same
    // _fixture (and therefore the same InstanceId), because xUnit instantiated
    // SeededAccountsFixture exactly once for the class.
    Assert.NotEqual(Guid.Empty, _fixture.InstanceId);
}
```

These tests are documentation, not interrogation. The real proof would be running them together and observing both fixtures match — but you can't assert "the value is the same one PartOne saw" from inside a fresh test method because there's no shared state to compare against.

A more direct way to see it in action: set a breakpoint in the `SeededAccountsFixture` constructor and run the whole `AccountQueryTests` class with `Shift+F9`. The breakpoint hits **once** — five tests, one fixture construction. Compare to setting a breakpoint in `AccountQueryTests`'s constructor: hits five times. That's the lifecycle made visible.

(You can also add a `_constructionCount` static field on the fixture, but it gets fragile under parallelism — Phase 8's territory. Skip that for now.)

**Optional immutability discipline:** Mark fixture-held state as `init`-only or otherwise non-mutable from outside. If a test could call `_fixture.Repository.OpenAccount(...)`, that mutation persists across subsequent tests in the class — which is at best surprising and at worst a parallelism bug waiting to happen. For this fixture, you might decide to keep `Repository` mutable (it has to be — `OpenAccount` is how accounts get in) but treat the convention as "tests don't mutate the fixture." Documenting that intent with a comment on the property is reasonable.

### Step 6 — Promote to `ICollectionFixture<T>` for cross-class sharing  `[ ]`

`IClassFixture<T>` gives each *class* its own fixture instance. If two test classes both write `: IClassFixture<SeededAccountsFixture>`, they get **two different** SeededAccountsFixture instances — one per class. Fine if the work is cheap; wasteful if the work is genuinely expensive.

To share *one* fixture across multiple classes, you need three pieces:

**1. A marker class implementing `ICollectionFixture<T>`** (no body needed — it's pure metadata):

```csharp
[CollectionDefinition("Seeded accounts")]
public class SeededAccountsCollection : ICollectionFixture<SeededAccountsFixture>
{
    // Intentionally empty. The marker exists only so xUnit can find the
    // [CollectionDefinition] and its ICollectionFixture<T> declaration.
}
```

**2. Apply `[Collection("name")]` to each test class that should share:**

```csharp
[Collection("Seeded accounts")]
public class AccountQueryTests
{
    private readonly SeededAccountsFixture _fixture;

    public AccountQueryTests(SeededAccountsFixture fixture)
    {
        _fixture = fixture;
    }
    // ... tests
}
```

**3. Remove `IClassFixture<SeededAccountsFixture>`** from the class declaration. Once the class is in a collection that ships the fixture, the IClassFixture wiring is redundant (and actually an error — you can't declare both for the same fixture).

Add a second class to demonstrate sharing — even a minimal one:

```csharp
[Collection("Seeded accounts")]
public class AccountInventoryTests
{
    private readonly SeededAccountsFixture _fixture;

    public AccountInventoryTests(SeededAccountsFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void Inventory_AfterSeeding_ContainsThreeAccounts()
    {
        Assert.True(_fixture.Repository.Contains(_fixture.Checking.Id));
        Assert.True(_fixture.Repository.Contains(_fixture.Savings.Id));
        Assert.True(_fixture.Repository.Contains(_fixture.Empty.Id));
    }
}
```

Run. All tests green. The breakpoint check from Step 5 still works: SeededAccountsFixture's constructor now fires **once for the entire collection**, not once per class.

**A side effect to know about:** classes sharing a collection also serialize with each other under xUnit's default parallelism — tests in the same collection don't run in parallel. We'll explore the parallelism implications in Phase 8; for now, just know it's a trade-off: sharing a fixture also means sharing a serialization boundary.

**NUnit ↔ xUnit:** `[SetUpFixture]` (assembly-level, namespace-scoped) → `ICollectionFixture<T>` + `[CollectionDefinition]`. NUnit's mechanism is opt-out (a `[SetUpFixture]` runs for any class in its namespace); xUnit's is opt-in (each class names the collection explicitly). The xUnit form is more verbose but more honest — you can't accidentally inherit shared setup just by living in a namespace.

### Step 7 — Decision table: which to reach for  `[ ]`

A quick summary worth internalizing:

| Setup need | xUnit answer |
|---|---|
| Per-test, cheap | Constructor (Phase 2 default) |
| Per-test, async | `IAsyncLifetime.InitializeAsync` (Phase 6) |
| Per-class, cheap-to-expensive | `IClassFixture<T>` |
| Per-class, async-to-set-up | `IClassFixture<T>` where `T : IAsyncLifetime` |
| Across multiple classes | `[CollectionDefinition]` + `ICollectionFixture<T>` + `[Collection("...")]` on each |
| Assembly-wide (xUnit v3) | `[assembly: AssemblyFixture(typeof(T))]` (a v3 addition; brief mention only — uncommon outside large suites) |

The escalation order is per-test → per-class → per-collection → per-assembly, each step trading away isolation for sharing. Start at the lowest level that works; promote only when sharing is actually useful.

### Step 8 — Refactor: extract the fixture (per the inline-then-extract pattern)  `[ ]`

`SeededAccountsFixture` is currently inline in `AccountQueryTests.cs` (per Step 3). Now that you understand the shape and have a second class consuming it (Step 6), extract it to its own file.

Two reasonable destinations:

- **`tests/Ledger.Tests/SeededAccountsFixture.cs`** — flat in the test root, alongside other test infrastructure. Fine for one or two fixtures.
- **`tests/Ledger.Tests/Fixtures/SeededAccountsFixture.cs`** — start a folder convention. Good when you anticipate several fixtures.

Either is defensible. The Infrastructure/ folder from the serializer work is for *test infrastructure that isn't a fixture* (serializers, helpers, custom assertions). Fixtures get their own home; Fixtures/ reads cleanest.

After extracting:

- The `SeededAccountsCollection` marker class can stay inline in `AccountQueryTests.cs` (where it's currently visible to both consuming classes via the assembly), or move alongside the fixture in `Fixtures/`. Your call. I'd move it — keeps related concepts together.
- No `using` directives needed for tests in `Ledger.Tests` to reach `Ledger.Tests.Fixtures` types unless you nest the namespace. If you put the fixture in `namespace Ledger.Tests.Fixtures`, tests will need `using Ledger.Tests.Fixtures;`. If you keep it in `namespace Ledger.Tests`, no using needed. Either works; namespace-matches-folder is the conventional choice.

Run the full suite. All tests stay green — pure structural refactor.

### Step 9 — NUnit↔xUnit lifecycle reflection  `[ ]`

In **Notes & questions** below, capture:

- How `IClassFixture<T>` compares to NUnit's `[OneTimeSetUp]` in terms of expressiveness. The xUnit form spreads the work across three pieces (fixture class + interface marker + constructor injection); NUnit's is a single attribute. What did you trade and what did you gain?
- Whether `[CollectionDefinition]` + `[Collection]` feels heavier than NUnit's `[SetUpFixture]`, and whether the explicitness pays off.
- Any tests in your prior NUnit codebases where `[OneTimeSetUp]` was being abused as "shared mutable state" — and what xUnit's design would force you to do instead.

---

## Stretch (optional)

- **Add a deliberate delay** to `SeededAccountsFixture`'s constructor (e.g., `Thread.Sleep(500)`) to simulate expensive setup. Run the suite with and without the fixture and observe the timing — `dotnet test` reports total time, or use Rider's profiler for a real measurement. Concretizes "why fixtures."
- **Assembly fixtures (xUnit v3)**: a brief look at `[assembly: AssemblyFixture(typeof(T))]`. The mechanism is straightforward but the use case is narrow — large suites where one infrastructure-piece (test database, container, etc.) is shared by everything. Worth knowing the door exists; rarely the right answer.
- **`IAsyncLifetime` on a fixture**: implement async setup in `SeededAccountsFixture.InitializeAsync` (and async teardown in `DisposeAsync`). xUnit calls these around the fixture's lifetime. We'll exercise this for real when persistence becomes async in Phase 6.
- **Read the xUnit author's notes on fixtures**: they often explain *why* a feature was designed a particular way, which sticks better than the API surface.

---

## Notes & questions

_Fill in as you go._

-
