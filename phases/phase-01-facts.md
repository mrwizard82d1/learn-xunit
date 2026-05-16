# Phase 1 — `[Fact]` and the assertion library (TDD)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Tour xUnit's assertion surface and lock in the NUnit→xUnit translations through TDD against `Money` (decimal amount + currency code).

## Decisions made

- **Smoke tests as permanent canaries.** `SmokeTests.cs` is the project-level canary; each new test class gets a per-class smoke `[Fact]` kept alongside the real tests.
- *(add others as we go)*

---

## Steps

### Step 1 — Per-class smoke for `MoneyTests`  `[ ]`

Create `tests/Ledger.Tests/MoneyTests.cs` with one smoke `[Fact]` — `Assert.Fail("smoke")` (or `Assert.True(false, "smoke")` if your xUnit version lacks `Assert.Fail`). Run, confirm fail. Flip to `Assert.True(true)`, confirm pass. Keep as the first `[Fact]` in the class.

xUnit-specific bits before Step 2:

- `[Fact]` instead of `[Test]`. No `[TestFixture]` — any public class with `[Fact]`/`[Theory]` is a test class.
- `using Xunit;` is implicit via `<Using Include="Xunit" />` in the csproj.
- `Assert.Equal` rather than `Assert.AreEqual`. Argument order is the same (`expected, actual`) as classic NUnit — no surprise for your muscle memory.

### Step 2 — RED: equality test + `Money` as a class  `[ ]`

Test: two `Money` values with the same amount and currency are equal. Use `Assert.Equal(expected, actual)`.

Pair with a compilable skeleton in `src/Ledger/Money.cs`:

```csharp
namespace Ledger;

public class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}
```

Class (not record) — the failing test drives the design choice. Default `class` equality is reference equality; the assertion fails.

### Step 3 — GREEN: convert to record  `[ ]`

```csharp
namespace Ledger;

public record Money(decimal Amount, string Currency);
```

Records generate `Equals`/`GetHashCode` from positional properties. Test passes.

### Step 4 — Catalog the failure message format  `[ ]`

Break the equality test on purpose (change the expected value) and read xUnit's failure output. Verify it's `Expected: X / Actual: Y` so the message reads fluently when it shows up in CI later. Revert.

(If you've used NUnit's `Assert.That(actual, Is.EqualTo(expected))` constraint API, the flipped argument order there is a real pitfall — but that's a post-2009 API you predate, so it's only relevant if you pair with someone who has that muscle memory.)

### Step 5 — RED → GREEN: `Add` happy path  `[ ]`

Test: `money1.Add(money2)` returns a new `Money` with summed amount, same currency.

Skeleton:

```csharp
public Money Add(Money other) => throw new NotImplementedException();
```

Red on `NotImplementedException`. Implement:

```csharp
public Money Add(Money other) => new(Amount + other.Amount, Currency);
```

### Step 6 — RED → GREEN: `Add` rejects currency mismatch  `[ ]`

Test:

```csharp
var ex = Assert.Throws<InvalidOperationException>(() => money1.Add(money2));
```

with different currencies. `Assert.Throws<T>` returns the caught exception (you'll chain assertions on `ex.Message` in Step 8).

xUnit specifics:

- `Assert.Throws<T>` requires the **exact** type. `Assert.ThrowsAny<T>` allows derived types.
- **No `Assert.DoesNotThrow`** — to test "doesn't throw," just call the code; an unhandled exception fails the test.

Add the guard, green.

### Step 7 — Your turn: `Subtract`  `[ ]`

Same shape as `Add`: two tests, two cycles. After Green 2, decide whether the shared currency-check between `Add` and `Subtract` is worth extracting yet.

### Step 8 — Assertion vocabulary  `[ ]`

Properties of `Money` worth verifying. One `[Fact]` per row.

| Property | Assertion |
|---|---|
| Two `Money` with different amounts are not equal | `Assert.NotEqual` |
| `Money.Amount > 0` for a constructed positive value | `Assert.True` / `Assert.False` |
| A nullable reference behaves as expected | `Assert.Null` / `Assert.NotNull` |
| Two `Equal` `Money` values are not the same instance | `Assert.Same` / `Assert.NotSame` |
| `Add` returns a `Money` | `Assert.IsType<Money>` |
| `Add` returns something assignable to `object` | `Assert.IsAssignableFrom<object>` |
| Currency-mismatch exception message contains `"USD"` | `Assert.Contains("USD", ex.Message)` |
| Currency-mismatch exception message starts with `"Cannot add"` | `Assert.StartsWith("Cannot add", ex.Message)` |
| Two `Money` built differently with same property values | `Assert.Equivalent` |

xUnit's equality distinctions worth noting:

- `Assert.Equal` — `IEquatable<T>` / structural (what records give you).
- `Assert.Same` — `ReferenceEquals`. Two records can be `Equal` without being `Same`.
- `Assert.Equivalent` — public-property comparison via reflection. Redundant for records; useful for legacy types without `IEquatable<T>`.

### Step 9 — NUnit→xUnit notes that actually mattered  `[ ]`

Add to **Notes & questions** below: which translations actually tripped you here, vs which were no-ops. Useful baseline for Phase 2, where the bigger lifecycle shifts live.

---

## Stretch (optional)

- Decompile `Xunit.Assert.Equal` (`Ctrl+B` → "Decompile") and look at the `IEnumerable<T>` handling — element-wise, where NUnit's `CollectionAssert.AreEqual` lives in a separate class.
- Check `Assert.Equivalent`'s reflection walk.
- `Assert.Multiple` in xUnit v3 — bundles multiple asserts so all failures report.

---

## Notes & questions

_Fill in as you go._

-
