# Phase 5 — Output, traits, skipping

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Three practical knobs that come up in real test maintenance, not in tutorials:

- **`ITestOutputHelper`** for diagnostic output that survives the runner's silence-by-default.
- **`[Trait]`** for tagging tests by category, slowness, or any axis you care to filter on — both from the CLI (`dotnet test --filter`) and (when it works) Rider's Test Explorer.
- **`[Fact(Skip = "…")]`** and `Assert.Skip*` for marking tests as skipped — conditionally or otherwise — *honestly*. The result is "Skipped" with a reason, not silently passing.

Each is small in isolation. Together they're what makes a test suite *operable* at scale.

This phase mostly demonstrates patterns rather than driving design. Steps still smoke-test new classes and verify behavior, but the rhythm is "wire it up, see it work" more than "drive the next contract."

## Decisions made

- (inherits Phase 1-4 patterns: smoke as canary, inline-then-extract, CurrencyCode for currency, fixture-as-immutable-seed, etc.)
- *(add others as we go)*

---

## Steps

### Step 1 — Per-class smoke for `AccountScenarioTests`  `[x]`

A new test class to host the ITestOutputHelper demonstration. Per the canary pattern:

```csharp
namespace Ledger.Tests;

public class AccountScenarioTests
{
    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);
}
```

Run. One green.

### Step 2 — Inject `ITestOutputHelper` and write a multi-step scenario  `[x]`

xUnit injects `ITestOutputHelper` into the test class constructor automatically — same DI-via-constructor pattern as fixtures. Add the dependency:

```csharp
public class AccountScenarioTests
{
    private readonly ITestOutputHelper _output;

    public AccountScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void SmokeTest() => Assert.Equal(4, 2 + 2);

    [Fact]
    public void OpenSeveralAccounts_QueryEach_AllPresent()
    {
        var repo = new AccountRepository();
        var usd = new CurrencyCode("USD");

        var checking = repo.OpenAccount(new Money(1000M, usd));
        _output.WriteLine($"Opened checking: {checking.Id}");

        var savings = repo.OpenAccount(new Money(5000M, usd));
        _output.WriteLine($"Opened savings: {savings.Id}");

        Assert.True(repo.Contains(checking.Id));
        Assert.True(repo.Contains(savings.Id));

        _output.WriteLine("Both accounts verified present.");
    }
}
```

Run. Both tests green. The new `[Fact]` produces output via `_output.WriteLine` — but **you may not see it by default** depending on how you ran the tests. That's Step 3's investigation.

`ITestOutputHelper` is xUnit's answer to "where did my `Console.WriteLine` go?" The runner captures all output per-test (so parallel tests don't interleave each other's logs) and surfaces it via the assertion-failure path or verbose-logging path.

**NUnit ↔ xUnit:** `Console.WriteLine` in NUnit works — output is collected and printed under per-test sections. In xUnit, `Console.WriteLine` is *captured but invisible* — xUnit refuses to surface it because parallel tests would interleave. `ITestOutputHelper` is the xUnit-correct way to log; it knows which test it belongs to.

`ITestOutputHelper`'s API is small: just `WriteLine(string)` and a few overloads. There's no `Write` (no partial line) — each call is a complete line. The interface is in `Xunit` (not `Xunit.Sdk`), so no additional using is needed beyond what your test files already have.

### Step 3 — Observe verbosity differences  `[ ]`

Run the suite at two verbosity levels and observe what's printed for the scenario test:

```
dotnet test
dotnet test --logger "console;verbosity=detailed"
```

The default omits per-test output for *passing* tests. Detailed surfaces it. To verify output capture works when a test fails, deliberately flip an assertion in `OpenSeveralAccounts_QueryEach_AllPresent` (e.g., `Assert.False(repo.Contains(checking.Id))`), run, observe the `_output.WriteLine` lines in the failure block. Revert when done.

The defaults reflect different priorities:

- **Quiet logs are CI-friendly** — successful runs produce minimal noise; failures get full diagnostic detail.
- **Verbose logs help local triage** — you want to see what tests produced during normal exploration without artificially failing them.

Knowing the flag exists is the practical bit. `xunit.runner.json` has settings that affect related behavior (`diagnosticMessages`, `internalDiagnosticMessages`) — Phase 8 explores these.

### Step 4 — Apply `[Trait]` to one smoke and demonstrate filtering  `[ ]`

`[Trait]` is a key-value tag you can put on any test method or test class. xUnit makes no assumptions about trait names — convention is yours to set.

A practical first application: tag a smoke test with `[Trait("Category", "Smoke")]`. Apply to `AccountScenarioTests.SmokeTest` first:

```csharp
[Fact]
[Trait("Category", "Smoke")]
public void SmokeTest() => Assert.Equal(4, 2 + 2);
```

Then run with the filter:

```
dotnet test --filter "Category=Smoke"
```

You should see exactly **one** test run — the smoke you tagged. Every other test (including the other smokes you haven't tagged yet) is excluded.

**Filter syntax** is documented as part of `vstest`'s expression grammar:

- `Category=Smoke` — exactly equal
- `Category!=Smoke` — not equal
- `Category=Smoke|Category=Fast` — OR (matches either)
- `Category=Smoke&Owner=me` — AND (matches both)
- `FullyQualifiedName~MoneyAdd` — substring match on test name

The same syntax works in `dotnet test --filter`, in `vstest.console`, and in IDE test runners that consume vstest filters.

**NUnit ↔ xUnit:** `[Category("Integration")]` → `[Trait("Category", "Integration")]`. NUnit's `[Category]` is single-valued; xUnit's `[Trait]` is key-value, which lets you tag along multiple orthogonal axes on the same test (e.g., a test could be `("Category", "Integration")` AND `("Owner", "billing-team")` AND `("Speed", "slow")`).

### Step 5 — Apply `[Trait("Category", "Smoke")]` across the suite (refactor)  `[ ]`

Tag every `SmokeTest()` method in every test class with `[Trait("Category", "Smoke")]`. There are several — `AccountQueryTests`, `AccountInventoryTests`, `AccountRepositoryTests`, `AccountScenarioTests`, `CurrencyCodeTests`, `MoneyAddTests`, `MoneyConstructorTests`, `MoneyEqualityTests`, `MoneyNegateTests`, `MoneySubtractTests`, `MoneyArithmeticInvariantsTests`, the standalone `SmokeTest` class. Rider's structural search (`Ctrl+Shift+M`) can do this in one operation if you want to automate; or walk through manually.

Once done:

```
dotnet test --filter "Category=Smoke"
```

…runs the full canary set. This becomes useful as:

- A fast pre-commit sanity check ("everything basically wired up?") distinct from running the full suite.
- A CI smoke stage that runs before the full test suite — fail fast if the basics broke.
- A quick "is the build healthy?" check during refactoring across files.

Other trait axes worth knowing about for later (don't add yet):

- `[Trait("Category", "Slow")]` for tests >1s — `--filter "Category!=Slow"` for fast local runs.
- `[Trait("Category", "Integration")]` once integration tests exist (Phase 9).
- `[Trait("Owner", "billing-team")]` if responsibility ownership matters in a larger team.

Trait *names* are conventional, not enforced. Pick what gives you useful filters; don't over-engineer.

### Step 6 — Demonstrate unconditional skip  `[ ]`

The simplest skip is attribute-level:

```csharp
[Fact(Skip = "Demonstration — not a real test")]
public void IntentionallySkipped_DemonstratesSkipBehavior()
{
    Assert.True(false); // never executes
}
```

Add this to `AccountScenarioTests`. Run:

```
dotnet test
```

You should see one test reported as "Skipped" with the reason. The body never executes — `Assert.True(false)` doesn't fire.

**This is honest behavior.** A skipped test is reported as a distinct outcome, *not* as a passing test. CI tooling can count skips separately from passes; reviewers see the reason in the test output. Compare to two common alternatives:

- **Comment out the test** — silent disappearance; no one knows it exists.
- **Delete the test** — the intent is lost; future-you doesn't know there *was* a deferred test here.

`[Fact(Skip = "reason")]` keeps the test in the suite, visibly skipped, with a reason. Skip when you don't know how to fix something yet but don't want to lose the test; delete when the test is no longer relevant.

**NUnit ↔ xUnit:** `[Ignore("reason")]` → `[Fact(Skip = "reason")]`. Same semantics; same honesty-as-default.

### Step 7 — Demonstrate conditional skip with `Assert.Skip*`  `[ ]`

Sometimes you want to skip *at runtime* based on a condition: OS, environment variable, network availability, optional dependency presence. xUnit v3 added native `Assert.Skip*` methods for this:

```csharp
[Fact]
public void EnvironmentSpecific_SkipsOutsideCi()
{
    Assert.SkipUnless(
        Environment.GetEnvironmentVariable("CI") == "true",
        "This test only runs in CI (where the environment is reproducible).");

    // ... real test body that depends on the CI environment ...
    Assert.True(true);
}
```

The `Assert.Skip*` family (xUnit v3):

- `Assert.Skip("reason")` — unconditional skip from within a test method. Rare; usually `[Fact(Skip = ...)]` is cleaner because the decision happens at discovery, not execution.
- `Assert.SkipWhen(condition, "reason")` — skips if condition is true.
- `Assert.SkipUnless(condition, "reason")` — skips if condition is false (the common form for "skip unless prerequisite is met").

The exact API names may vary slightly across xUnit v3 minor versions; if your IDE flags a signature mismatch, check the version of `xunit.v3.core` you're pulling in and adjust.

Behavior is the same as `[Fact(Skip = ...)]`: the test reports as Skipped with the given reason. The difference is *when* the decision happens:

- **`[Fact(Skip)]`** — discovery time. The test is always skipped; the runner never tries to execute it.
- **`Assert.Skip*`** — runtime. The test starts executing; the skip call short-circuits the rest of the body.

Run with and without the `CI` environment variable set:

```
dotnet test --filter "Name=EnvironmentSpecific_SkipsOutsideCi"
# Skipped: "This test only runs in CI..."

CI=true dotnet test --filter "Name=EnvironmentSpecific_SkipsOutsideCi"
# Runs (and passes)
```

**Pre-v3 alternative for context:** older xUnit versions didn't have `Assert.Skip*` natively. The community solution was the `Xunit.SkippableFact` package, which added a `[SkippableFact]` attribute and a `Skip.If(...)` method that threw a `SkipException`. With xUnit v3, none of that is needed — `Assert.Skip*` is built in. If you encounter `Xunit.SkippableFact` in older codebases, the v3 migration replaces it with native `Assert.Skip*` calls.

**NUnit ↔ xUnit:** NUnit's `Assert.Ignore("reason")` is the closest equivalent. Same "skip-at-runtime-with-honest-reporting" pattern.

### Step 8 — NUnit↔xUnit output/traits/skip reflection  `[ ]`

In **Notes & questions** below, capture:

- How often you reached for `Console.WriteLine` in NUnit tests and whether `ITestOutputHelper` feels heavier-but-cleaner, or just heavier.
- Whether the `[Trait]` key-value form gives you anything beyond what NUnit's single-valued `[Category]` did, or whether it's the same idea with a different shape.
- Whether xUnit's "Skipped is a real outcome" philosophy matches how you've thought about ignored tests in NUnit, or whether you tended to delete-and-forget instead.

---

## Stretch (optional)

- **Read the [vstest filter syntax docs](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests).** Boolean combinators, parentheses, and pattern-matching operators compose surprisingly expressive filters. Worth knowing when CI starts wanting "run tests in folder X but not those tagged Slow, and only if they belong to owner Y."
- **`xunit.runner.json` output and diagnostic settings**: explore `methodDisplay`, `methodDisplayOptions`, `diagnosticMessages`, `internalDiagnosticMessages`. Small knobs that change how Rider and CLI runners display test results. We'll come back to this in Phase 8.
- **Capture output from a *failing* test under `dotnet test`** and observe that it prints only on failure by default — a deliberate signal-to-noise choice. Compare to Rider's behavior of always showing output. Both are defensible; knowing the difference saves confusion when results "look different" between local and CI runs.
- **A custom `ITraitDiscoverer`** — write a tiny one that derives traits from test method naming conventions (e.g., any method whose name starts with `Smoke_` automatically gets `Category=Smoke`). Niche, but a good window into xUnit's extension points.

---

## Notes & questions

_Fill in as you go._

-
