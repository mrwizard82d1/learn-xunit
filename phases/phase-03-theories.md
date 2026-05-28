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

### Step 2 — REFACTOR:~~~~ `[InlineData]` with multiple parameters  ~~~~`[x]`

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

### Step 3 — REFACTOR: `[MemberData]` when `[InlineData]` can't carry the input shape  `[x]`

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
public static TheoryData<decimal, decimal, decimal, string> AddCases =>
    new()
    {
        { 607.37M, 733.74M, 1341.11M, "DKK" },
        { 871.13M, -892.52M, -21.39M, "JPY" },
        { 0M, 913.38M, 913.38M, "BWP" },
    };

[Theory]
[MemberData(nameof(AddCases))]
public void Add_ProducesExpectedSum(decimal a, decimal b, decimal expectedSum, string currency)
{
    var actual = new Money(a, currency).Add(new Money(b, currency));
    Assert.Equal(new Money(expectedSum, currency), actual);
}
```

`TheoryData<>` ships in xUnit with overloads up to about 10 type parameters; pick the arity matching your test method. Same `[MemberData(nameof(AddCases))]` usage as Step 3 — only the source's *type* has changed. The compiler now rejects shape mistakes the `IEnumerable<object[]>` form would have hidden until runtime.

**A note on the currency column.** `Add`'s arithmetic is currency-agnostic — the same logic runs whether the inputs are DKK, JPY, or BWP. You could hold currency constant ("DKK" in every case) and the arithmetic coverage would be identical. The reason to *vary* it across cases is safety-net: if a future change to `Add` introduces a currency-specific branch (special handling for USD, different rounding for EUR), tests fixed on a single currency would silently miss it. Variance is cheap insurance, and it doesn't muddy the focus argument — if `Add_ProducesExpectedSum(addend1: ..., currency: "JPY")` fails, the case name still points you at exactly one input combination. Choose representative currencies once; don't randomize per run (you'd lose reproducibility for negligible gain over fixed variance).

**Why primitives instead of Money?** You might reasonably want `TheoryData<Money, Money, Money>` — the data would read more directly as intent ("these two Moneys sum to this one"). It compiles and runs. But xUnit emits warning `xUnit1045` ("type arguments aren't serializable") on the declaration, because `Money` isn't a type xUnit's built-in serializer recognizes. Step 5 explains that warning and shows how to make it go away. For Step 4's purpose — *compile-time type safety* — primitives suffice.

**`[ClassData]`** is for when test data deserves its own class — typically because it's reused across multiple test classes or because construction is non-trivial:

```csharp
public class AddTestData : TheoryData<decimal, decimal, decimal, string>
{
    public AddTestData()
    {
        Add(607.37M, 733.74M, 1341.11M, "DKK");
        Add(871.13M, -892.52M, -21.39M, "JPY");
    }
}

[Theory]
[ClassData(typeof(AddTestData))]
public void Add_ProducesExpectedSum(decimal a, decimal b, decimal expectedSum, string currency) { /* ... */ }
```

**When to reach for `[ClassData]` vs a static `TheoryData<>` member:**

- **Static `TheoryData<>` property** for one-off, local data. Cleaner — no extra class.
- **`[ClassData]` with a `TheoryData<>` subclass** when the data is reused across files, non-trivial to construct (loaded from JSON/CSV, computed from a Cartesian product), or you want it in a different file from the tests.

For the Add cases in your Money suite, a static `TheoryData<...>` property is the right call. `[ClassData]` earns its keep at larger scale.

### Step 5 — Theory case serialization: rules, `IXunitSerializer`, and Money-typed `TheoryData`  `[ ]`

Step 4 left a question hanging: why does `TheoryData<Money, Money, Money>` produce a warning when `TheoryData<decimal, decimal, decimal, string>` doesn't? Answer: xUnit serializes each `[Theory]` case's arguments so the runner can give the case a stable identity and a readable display name. If a parameter type isn't serializable, that machinery breaks down.

#### What xUnit serializes natively

Built-in support in xUnit v3:

- `bool`, all numeric primitives (including `decimal`), `string`, `char`, `enum`.
- `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `BigInteger`, `Index`, `Range`.
- `Type` (passed as `typeof(...)`).
- Arrays of any supported type. Nullable variants of all of these.

Not natively supported: arbitrary records, classes, structs, tuples — even when every field is serializable individually. xUnit doesn't recursively introspect; it works from a fixed list and explicit extension points.

`Money` is a record whose fields (`decimal`, `string`) are both supported, but xUnit doesn't infer the trip. That's the source of the `xUnit1045` warning on `TheoryData<Money, Money, Money>`.

#### What happens when a type isn't serializable

The test cases still run and still pass or fail correctly. What you lose is per-case display:

```
✗ Add_ProducesExpectedSum [3 cases]
```

instead of:

```
✓ Add_ProducesExpectedSum(addend1: Money{Amount=607.37, Currency=DKK}, addend2: ..., expected: ...)
✓ Add_ProducesExpectedSum(addend1: Money{Amount=871.13, Currency=JPY}, ...)
✓ Add_ProducesExpectedSum(addend1: Money{Amount=0, Currency=BWP}, ...)
```

Tolerable for three cases; painful at scale.

#### Two extension interfaces

xUnit v3 offers two ways to extend the supported set:

- **`IXunitSerializable`** — the *type itself* implements the interface. Two methods: `Serialize(IXunitSerializationInfo info)` writes the fields, `Deserialize(IXunitSerializationInfo info)` reads them back. Works, but pulls an xUnit dependency into the production type and requires a parameterless constructor — awkward for a record with positional init-only properties.
- **`IXunitSerializer`** — a *separate class* implements the interface and is registered with an assembly attribute. Keeps the production type completely clean. This is the v3-native approach for types you don't want to modify.

We'll use the second one. `Money` stays untouched.

#### Hands-on: writing `MoneyXunitSerializer`

Create a new file in the test project (e.g., `tests/Ledger.Tests/Infrastructure/MoneyXunitSerializer.cs`). Implement `Xunit.Sdk.IXunitSerializer` with three methods:

- `bool IsSerializable(Type type, object? value, out string? failureReason)` — return `true` if `type == typeof(Money)`, `false` otherwise. When returning `false`, set `failureReason` to a short explanation; when `true`, set it to `null`.
- `string Serialize(object value)` — convert a `Money` to a stable string. A simple separator-based format works for two fields: `$"{money.Amount.ToString(CultureInfo.InvariantCulture)}|{money.Currency}"`. Use `CultureInfo.InvariantCulture` for the decimal — without it, machines with different locales would format `.` vs `,` differently and break round-trips.
- `object Deserialize(Type type, string serializedValue)` — reverse the format. Split on `|`, parse the first part as `decimal` with `CultureInfo.InvariantCulture`, take the second part as the currency string, return `new Money(amount, currency)`.

Register the serializer with an assembly-level attribute. Conventional placement is at the top of the serializer file, outside any namespace:

```csharp
[assembly: Xunit.Sdk.RegisterXunitSerializer(typeof(MoneyXunitSerializer), typeof(Money))]
```

**Heads-up on the API surface:** `IXunitSerializer` and `RegisterXunitSerializer` both live in `Xunit.Sdk`. This is the less-public, more-volatile area of xUnit — the everyday assertion/attribute surface lives elsewhere. Worth knowing you've stepped one layer deeper. The exact interface shape has had minor revisions across xUnit v3 versions; if your IDE flags a signature mismatch, check the version of `xunit.v3.core` you're pulling in and adjust accordingly.

**On the simple separator format:** `|` happens to be safe for ISO 4217 currency codes (they're all uppercase letters). For more complex value types — strings that could contain anything, multiple fields, nested types — pick a more robust serialization (JSON, length-prefixed, etc.). The point isn't the format; it's the round-trip identity.

#### Round-trip the serializer

Before trusting it in test data, exercise it. A small test class is enough:

```csharp
public class MoneyXunitSerializerTests
{
    private readonly MoneyXunitSerializer _serializer = new();

    [Theory]
    [InlineData(607.37, "DKK")]
    [InlineData(-21.39, "JPY")]
    [InlineData(0, "USD")]
    public void Serializer_RoundTripsMoneyToEqualMoney(decimal amount, string currency)
    {
        var original = new Money(amount, currency);
        var serialized = _serializer.Serialize(original);
        var roundTripped = _serializer.Deserialize(typeof(Money), serialized);
        Assert.Equal(original, roundTripped);
    }
}
```

Worth adding a case for `IsSerializable` returning `false` for a non-`Money` type, too.

The round-trip test also doubles as drift insurance. If `Money` later grows a new field (say, `Description`), `Assert.Equal(original, roundTripped)` will fail — record equality is generated from all fields, so the deserialized value's missing field surfaces immediately. That's the test you want to catch a serializer that's silently fallen out of sync with the type it serializes.

#### Restore Money-typed `TheoryData<>`

With the serializer in place, change `AddCases` from `TheoryData<decimal, decimal, decimal, string>` back to `TheoryData<Money, Money, Money>` (the form you originally tried in Step 4):

```csharp
public static TheoryData<Money, Money, Money> AddCases =>
    new()
    {
        { new Money(607.37M, "DKK"), new Money(733.74M, "DKK"), new Money(1341.11M, "DKK") },
        { new Money(871.13M, "JPY"), new Money(-892.52M, "JPY"), new Money(-21.39M, "JPY") },
        { new Money(0M, "BWP"), new Money(913.38M, "BWP"), new Money(913.38M, "BWP") },
    };

[Theory]
[MemberData(nameof(AddCases))]
public void Add_ProducesExpectedSum(Money addend1, Money addend2, Money expected)
{
    Assert.Equal(expected, addend1.Add(addend2));
}
```

Three things to observe:

1. The `xUnit1045` warning is gone. xUnit now consults your registered serializer for `Money`.
2. Run the suite. Each `Add_ProducesExpectedSum` case displays individually in the runner output.
3. The test method body shrank back to a single assertion — no construction inside the test. The data table reads as intent: "these two Moneys sum to this one."

That last point is the actual win of this whole chain. Step 3 promised it; Step 4 took a step back to get type safety; Step 5 restores it with the serializer doing the bridging work behind the scenes.

#### When to remove the serializer

Treat `MoneyXunitSerializer` as **deletable scaffolding**. It exists because `Money`, as defined today, isn't a type xUnit's built-in serializer recognizes. If the type-to-xUnit relationship ever changes — for instance, if you migrate `Money` to implement `IXunitSerializable` directly — the bridge becomes obsolete and should be removed.

You don't want both at once:

- Both mechanisms can produce different serialized strings for the same `Money`, and xUnit will pick exactly one at runtime (verify the precedence rule against your xUnit version before relying on it). The unused mechanism becomes dead code that future readers must investigate.
- They can drift out of sync — the same case displaying or comparing differently depending on which path xUnit consults. Subtle failure mode.

For your situation, the external serializer is the right long-term home: it keeps `Money` xUnit-agnostic, which is the correct relationship between a domain type and a test framework. The only realistic scenario for moving to `IXunitSerializable` on `Money` itself is if you're publishing `Money` as a library and want consumers to get serialization "for free" — then the migration is atomic (delete the external serializer + assembly attribute, add `IXunitSerializable` to `Money`), never layered.

The drift you're actually defending against day-to-day isn't "Money gains `IXunitSerializable`." It's "Money grows a field that the serializer hasn't been updated to handle." The round-trip tests above catch that automatically as long as you compare full record equality.

#### Display name and parameter formatting (brief)

Custom display name for the parent `[Theory]`:

```csharp
[Theory(DisplayName = "Add: same currency produces expected sum")]
[MemberData(nameof(AddCases))]
public void Add_ProducesExpectedSum(...) { ... }
```

The per-case parameter values still get appended. For most tests, the default `MethodName(param1: value1, ...)` format is fine — leave it alone unless you have a specific reason.

How a parameter renders in the case name depends on its type's `ToString()` — `decimal` renders as the number, `string` renders as the literal, your `Money` renders as the record's auto-generated `Money { Amount = 607.37, Currency = DKK }` form. If you wanted to customize how `Money` displays in case names, you'd override `ToString()` on `Money` — independent of the serializer (serializer = identity; `ToString()` = display).

**NUnit ↔ xUnit:** NUnit auto-serializes test parameters by walking their public properties via reflection, which is why you've never had to think about this in NUnit. xUnit is more conservative on purpose — explicit beats magical when display names need to be stable across runs and processes. The cost is the occasional `IXunitSerializer` you have to write; the benefit is no surprises when complex types behave oddly in case identity.

### Step 6 — NUnit↔xUnit theories reflection  `[ ]`

In **Notes & questions** below, capture:

- Which of `[InlineData]` / `[MemberData]` / `[ClassData]` / `TheoryData<>` you'd reach for in your typical work scenarios.
- What `[Values]` and `[Combinatorial]` from NUnit do, and what you'd use in xUnit instead. (Typically: enumerate the combinations as multiple `[InlineData]`, or use a Cartesian-product helper exposed via `[MemberData]`. The third-party `Xunit.Combinatorial` package adds `[Values]`/`[CombinatorialData]` back if you find yourself reaching for them often.)
- Whether the parameter-values-in-test-names display was a positive or negative change after using it for a phase.

---

## Stretch (optional)

- **`Xunit.Combinatorial`** NuGet package — adds `[Values]` and `[CombinatorialData]` to xUnit, mimicking NUnit's API for Cartesian-product test cases. Useful when the alternative is hand-enumerating dozens of `[InlineData]` lines.
- **`[MemberData]` with parameters:** the referenced member can be a method that takes arguments passed via `[MemberData(nameof(Method), param1, param2)]`. Niche but occasionally useful for data that needs to be parameterized itself.

---

## Notes & questions

_Fill in as you go._

-
