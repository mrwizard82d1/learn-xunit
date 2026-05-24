# Phase 3 — `[Theory]`, `[InlineData]`, `[MemberData]`, `[ClassData]` (TDD)

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `` `[ ]` ``. Flip to `` `[x]` `` when complete.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** `Ledger.Tests` Run configuration (Rider) or `dotnet test` (CLI).

## Goal

Translate `[TestCase]` muscle memory and pick the right xUnit data source for the job. By the end:

- **`[InlineData]`** for compile-time-constant inputs (numbers, strings, enums, simple arrays).
- **`[MemberData]`** for inputs needing computation or types `[InlineData]` can't express (records, custom value types, dates).
- **`[ClassData]` / `TheoryData<T1, T2, ...>`** for strongly-typed, reusable test data classes.
- Understand why theory data must be **serializable** for the runner to display individual cases — and what happens when it isn't.

Vehicle: refactor existing Phase 1 tests where the shape is "same logic, different inputs." Most of the learning is collapsing existing tests, not writing new ones.

## Decisions made

- (inherits Phase 1 and Phase 2 patterns)
- *(add others as we go)*

---

## Steps

### Step 1 — REFACTOR: `[Theory]` + `[InlineData]` for the constructor amount tests  `[x]`

`MoneyConstructorTests` has three tests with identical shape varying only by amount:

- `ConstructCurrency_WithPositiveAmount_ReportsPositive`
- `ConstructCurrency_WithNegativeAmount_ReportsNegative`
- `ConstructCurrency_WithZeroAmount_ReportsZero`

Collapse them to one `[Theory]`:

```csharp
[Theory]
[InlineData(199.45)]
[InlineData(-439.45)]
[InlineData(0)]
public void Constructor_PreservesAmount(decimal amount)
{
    var money = new Money(amount, "USD");
    Assert.Equal(amount, money.Amount);
}
```

Run. You'll see **three test cases reported independently**, one per `[InlineData]`. Failure isolation is per-case: one case failing doesn't skip the others.

**NUnit ↔ xUnit:** `[TestCase(199.45)]` → `[InlineData(199.45)]`. Same shape, different attribute. The method-level pair `[Test]+[TestCase]` becomes `[Theory]+[InlineData]`. Note: a `[Theory]` *must* have at least one data attribute — there's no parameterless `[Theory]`.

**Practice candidates** (do these too, same pattern):

- `MoneyNegateTests`: positive/negative/zero amount cases — `[Theory] [InlineData(940.95, -940.95)] ... public void Negate_ReturnsOppositeAmount(decimal input, decimal expected)`.

### Step 2 — REFACTOR: `[InlineData]` with multiple parameters  `[ ]`

`MoneyAddTests` has several happy-path tests expressing Money sums. `[InlineData]` can't carry a `Money` (records aren't compile-time constants), but you can decompose to decimals + a currency string and reconstruct inside the method:

```csharp
[Theory]
[InlineData(607.37, 733.74, 1341.11)]   // sum positive
[InlineData(871.13, -892.52, -21.39)]   // negative second operand
[InlineData(0, 50, 50)]                  // first operand zero
public void Add_SameCurrency_ProducesExpectedSum(decimal a, decimal b, decimal expectedSum)
{
    var currency = "DKK";
    var actual = new Money(a, currency).Add(new Money(b, currency));
    Assert.Equal(new Money(expectedSum, currency), actual);
}
```

This works, and it's a legitimate pattern when the inputs decompose cleanly to primitives. The cost: the test method now does construction-and-arithmetic, which slightly obscures the "given these Moneys, expect this sum" intent. When the cost rises (mixed currencies, multiple Moneys per case, complex setup), `[MemberData]` is the next move (Step 3).

**Worth noting on the way:** `[InlineData]` arguments are constrained to whatever the C# attribute system allows — primitive types, strings, `typeof(T)`, enums, and arrays of those. No `new SomeClass(...)`, no `DateTime` literals (use `string` and `DateTime.Parse` inside the test), no `decimal` literal with `M` suffix (the attribute representation is `double` — the compiler converts).

### Step 3 — REFACTOR: `[MemberData]` when `[InlineData]` can't carry the input shape  `[ ]`

For the same Add cases, expressed as `Money` instances directly:

```csharp
public static IEnumerable<object[]> AddCases =>
    new[]
    {
        new object[] { new Money(607.37M, "DKK"), new Money(733.74M, "DKK"), new Money(1341.11M, "DKK") },
        new object[] { new Money(871.13M, "DKK"), new Money(-892.52M, "DKK"), new Money(-21.39M, "DKK") },
        new object[] { new Money(0M, "USD"), new Money(50M, "USD"), new Money(50M, "USD") },
    };

[Theory]
[MemberData(nameof(AddCases))]
public void Add_ProducesExpectedSum(Money first, Money second, Money expected)
{
    Assert.Equal(expected, first.Add(second));
}
```

Now the test reads as the intent says: given these two Moneys, expect this sum.

**Rules to remember:**

- `AddCases` must be `public static` — xUnit uses reflection on static members for data discovery.
- Return type: `IEnumerable<object[]>` (each inner `object[]` is one case's arguments). The newer typed alternative is `TheoryData<...>` (Step 4).
- `nameof(AddCases)` is refactor-safe; if you rename the property, the reference moves with it. Prefer over `"AddCases"` string literal.
- `[MemberData]` can also reference a method (with parameters) or a field, not just a property.

**NUnit ↔ xUnit:** `[TestCaseSource(nameof(AddCases))]` → `[MemberData(nameof(AddCases))]`. Direct translation; same idea.

### Step 4 — REFACTOR: `TheoryData<T1, T2, ...>` for type-safe data; `[ClassData]` for reusable sets  `[ ]`

`IEnumerable<object[]>` is untyped — a casting mistake in the data only surfaces at test runtime as an obscure `InvalidCastException`. `TheoryData<T1, T2, ...>` gives compile-time type safety:

```csharp
public static TheoryData<Money, Money, Money> AddCases =>
    new()
    {
        { new Money(607.37M, "DKK"), new Money(733.74M, "DKK"), new Money(1341.11M, "DKK") },
        { new Money(871.13M, "DKK"), new Money(-892.52M, "DKK"), new Money(-21.39M, "DKK") },
    };
```

Same `[MemberData(nameof(AddCases))]` usage. Now the types are enforced by the compiler. `TheoryData<>` ships in xUnit with overloads up to about 10 type parameters; use the arity matching your test method's parameter count.

**`[ClassData]`** is for when test data deserves its own class — typically because it's reused across multiple test classes or because construction is non-trivial:

```csharp
public class AddTestData : TheoryData<Money, Money, Money>
{
    public AddTestData()
    {
        Add(new Money(607.37M, "DKK"), new Money(733.74M, "DKK"), new Money(1341.11M, "DKK"));
        Add(new Money(871.13M, "DKK"), new Money(-892.52M, "DKK"), new Money(-21.39M, "DKK"));
    }
}

[Theory]
[ClassData(typeof(AddTestData))]
public void Add_ProducesExpectedSum(Money first, Money second, Money expected) { /* ... */ }
```

**When to reach for `[ClassData]` vs `TheoryData<>` member:**

- **Static `TheoryData<>` property** for one-off, local data. Cleaner — no extra class.
- **`[ClassData]` with a `TheoryData<>` subclass** when the data is reused across files, non-trivial to construct (loaded from JSON/CSV, computed from a Cartesian product), or you want it in a different file from the tests.

For the Add cases in your Money suite, a static `TheoryData<...>` property is the right call. `[ClassData]` earns its keep at larger scale.

### Step 5 — Display names, parameter formatting, and the serialization gotcha  `[ ]`

Run `dotnet test` (or check the Rider Test Explorer if it ever starts working for MTP — see Phase 0 decisions) and observe how `[Theory]` cases display:

```
MoneyConstructorTests.Constructor_PreservesAmount(amount: 199.45) [PASS]
MoneyConstructorTests.Constructor_PreservesAmount(amount: -439.45) [PASS]
MoneyConstructorTests.Constructor_PreservesAmount(amount: 0) [PASS]
```

The runner formats each case using the parameter values. This requires the values to be **serializable** by xUnit's serializer:

- Primitives, strings, enums, `Type`, `DateTime`, `DateTimeOffset`, `Guid`, `decimal` — all supported.
- Arrays of supported types — supported.
- Types implementing `IXunitSerializable` (xUnit v3) — supported.
- Records of supported types (like `Money`) — supported, because records serialize via their public properties.

**The gotcha:** if a parameter type isn't serializable, the runner can't display individual cases. They collapse into a single line:

```
MoneyAddTests.Add_ProducesExpectedSum [3 cases]
```

You still get one result per case, but you can't tell from the test output which input case failed — you have to read the failure message body. This usually bites when test data contains closures, delegates, or types with non-serializable fields.

**Customizing the display name** of the parent `[Theory]`:

```csharp
[Theory(DisplayName = "Add: same currency produces expected sum")]
[InlineData(...)]
public void Add_ProducesExpectedSum(...) { ... }
```

The per-case parameter values still get appended. For most tests, the default `MethodName(param1: value1, ...)` format is fine and you should leave it alone.

### Step 6 — NUnit↔xUnit theories reflection  `[ ]`

In **Notes & questions** below, capture:

- Which of `[InlineData]` / `[MemberData]` / `[ClassData]` / `TheoryData<>` you'd reach for in your typical work scenarios.
- What `[Values]` and `[Combinatorial]` from NUnit do, and what you'd use in xUnit instead. (Typically: enumerate the combinations as multiple `[InlineData]`, or use a Cartesian-product helper exposed via `[MemberData]`. The third-party `Xunit.Combinatorial` package adds `[Values]`/`[CombinatorialData]` back if you find yourself reaching for them often.)
- Whether the parameter-values-in-test-names display was a positive or negative change after using it for a phase.

---

## Stretch (optional)

- **`Xunit.Combinatorial`** NuGet package — adds `[Values]` and `[CombinatorialData]` to xUnit, mimicking NUnit's API for Cartesian-product test cases. Useful when the alternative is hand-enumerating dozens of `[InlineData]` lines.
- **Implementing `IXunitSerializable`** for a custom type so it displays nicely in theory case names. Worth doing once for a type you control to see the mechanics, even if you usually don't need it.
- **`[MemberData]` with parameters:** the referenced member can be a method that takes arguments passed via `[MemberData(nameof(Method), param1, param2)]`. Niche but occasionally useful for data that needs to be parameterized itself.

---

## Notes & questions

_Fill in as you go._

-
