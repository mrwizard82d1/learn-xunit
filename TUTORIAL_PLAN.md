# xUnit Tutorial Plan — for an NUnit-fluent developer using JetBrains Rider (IntelliJ keymap)

## Subject domain: Account Ledger

A small banking/ledger system. Familiar to everyone (accounts, deposits, withdrawals, transfers, interest, statements), and structurally rich enough that tests genuinely *need* the xUnit features we'll explore, instead of being engineered to show them off.

Core concepts we'll grow into the codebase as the tutorial progresses:

- **Money** — value object with currency; arithmetic with currency-mismatch rules.
- **Account** — has a balance, an overdraft limit, an account type (Checking, Savings).
- **Ledger** — append-only list of `Transaction` entries; balance is derived from the ledger.
- **TransferService** — moves money between two accounts atomically; rejects on insufficient funds, currency mismatch, or self-transfer.
- **InterestPolicy** — accrues interest on Savings based on a date range and rate schedule.
- **FraudPolicy** — flags suspicious patterns (rapid repeat withdrawals, large round-number transfers).
- **IAccountRepository / Async persistence** — async read/write for integration-style tests.

These map naturally onto every xUnit feature worth knowing without forcing it.

---

## How to use this plan

- It is a **map, not a script.** Detours are encouraged — most of the value in learning a tool comes from the questions you ask while using it.
- **TDD as the default learning rhythm.** Each new test class starts with a smoke `[Fact]` (a runnable failure, then flipped to passing) that is **kept as a permanent canary** for that class. Subsequent tests follow red-green-refactor where every "red" is a *runnable* assertion failure — **compile errors don't count as red**. When a new test needs not-yet-existent production code, pair it with a minimal compilable skeleton (`throw new NotImplementedException()`, a deliberately weaker implementation) so the test fails on assertion, not on build. Some phases focus on xUnit mechanics rather than driving design (Phase 4 fixtures, Phase 8 parallelism, Phase 10 migration) and relax the strict cycle.
- Each phase has: **Goal**, **What we'll build**, **xUnit features introduced**, and **NUnit ↔ xUnit notes** so your existing knowledge does the heavy lifting. **Rider notes** appear where the IDE materially changes the experience.
- Shortcuts cite the **IntelliJ-style keymap** (Rider → Settings → Keymap → "IntelliJ IDEA Classic" or similar). If a shortcut ever feels wrong, you've probably picked it up from Visual Studio muscle memory; check `Help → Find Action` (`Ctrl+Shift+A`).
- The "Stretch" bullets in each phase are optional rabbit holes; pull them in if they catch your interest.
- After each phase, we'll pause for questions before moving on.

Mark phases with `[x]` as you complete them so the file becomes a record of what you've learned.

> **Setup note:** This project uses Rider via **JetBrains Gateway Remote Development** with the Rider backend inside WSL2 Ubuntu. Build, edit, refactor, and debug all work; test execution uses the auto-generated **`Ledger.Tests` Run configuration**. However, **Rider's Test Explorer doesn't populate** for xUnit v3 + MTP on Rider 2026.1.1, so Rider notes in this plan that depend on **per-test gutter icons** or the **Tests/Unit Tests tool window** don't apply. Use the Run configuration for all-tests; `dotnet test --filter "..."` from the CLI for per-test runs. See [`phases/phase-00-setup.md`](phases/phase-00-setup.md) for full context.

---

## Phase 0 — Project setup & first run  `[x]`

**Goal:** Get a working solution with one passing and one failing test, and understand the runner ergonomics in both CLI and Rider.

- `dotnet new sln`, a class library `Ledger`, and `Ledger.Tests` via `dotnet new xunit`.
- Reference the right packages: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`. (We'll talk briefly about `xunit.v3` vs `xunit.v2` and what changed.)
- Run `dotnet test` from the CLI; then run from Rider; observe how the two surfaces differ.
- Write a deliberately failing test to see the failure formatting in both surfaces.

**NUnit ↔ xUnit notes**

- No `[TestFixture]` — every public class with `[Fact]`/`[Theory]` methods is a test class.
- `dotnet test` discovery is identical; the runner ecosystem is different.

**Rider notes**

- Rider auto-discovers xUnit; no plugin required. Open the **Unit Tests** tool window and the **Unit Test Sessions** tool window via `View → Tool Windows` (or `Ctrl+Shift+A` → "Unit Tests").
- The green/red gutter icons next to each `[Fact]`/`[Theory]` are your primary entry point: click to run, right-click for "Debug" / "Run with Coverage" / "Profile."
- `Shift+F10` runs the test under the caret (or last run config); `Shift+F9` debugs it; `Ctrl+Shift+F10` builds a run configuration from the current context — useful when the caret is on a class or namespace and you want to run that scope.
- Rider's **diff viewer** opens automatically when an `Assert.Equal` on strings or collections fails. This is one of the highest-leverage things Rider gives you over the CLI.

**Stretch:** Inspect what `xunit.runner.json` controls (we'll revisit in Phase 8). Rider picks up changes to it on rebuild.

---

## Phase 1 — `[Fact]` and the assertion library  `[ ]`

**Goal:** Translate your "happy path + a couple of edges" instinct from NUnit. Land on the assertion patterns you'll use 80% of the time.

- Build `Money` with `Add`, `Subtract`, currency check; write `[Fact]` tests for the simple invariants.
- Tour `Assert.Equal`, `Assert.NotEqual`, `Assert.True/False`, `Assert.Null`, `Assert.Same`, `Assert.Equivalent`, `Assert.Contains`, `Assert.StartsWith`, `Assert.IsType`, `Assert.IsAssignableFrom`.
- `Assert.Throws<T>` and `Assert.ThrowsAny<T>` (and why xUnit doesn't have `Assert.That`).

**NUnit ↔ xUnit notes** — these bite people:

- Argument order is `Assert.Equal(expected, actual)` — same as NUnit's classic API, **opposite** of NUnit's constraint-based assertions if you've trained on `Assert.That(actual, Is.EqualTo(expected))`. The xUnit message reads "Expected ... Actual ..." so a flipped argument produces backwards diagnostics.
- No `[TestCase]` on a `[Fact]`. A `[Fact]` takes no parameters. (That's Phase 3.)
- No `Assert.AreEqual` — just `Assert.Equal`.

**Rider notes**

- Type `Assert.` and let completion show you the surface area; it's smaller than NUnit's, which is the point.
- When an `Assert.Equal` fails on a record/POCO, Rider's diff viewer shows the structural difference. This makes `Assert.Equivalent` (which compares public properties) less necessary than it might otherwise be.
- `Alt+Enter` on a method name → "Create Test" scaffolds an xUnit test in the right project once Rider knows the test project pairing.
- `Ctrl+B` (Go To Declaration) on `Assert` to peek at the API; `Ctrl+Alt+B` for the implementation, which decompiles to show how a given assert actually works.

**Stretch:** Decompile `Xunit.Assert.Equal` to see how it handles `IEnumerable` element-wise — different from NUnit's `CollectionAssert`.

---

## Phase 2 — Lifecycle: constructor, `IDisposable`, `IAsyncLifetime`  `[x]`

**Goal:** Internalize the single biggest cultural difference from NUnit: **a fresh test class instance per test method.**

- Add an `InMemoryAccountRepository` and write tests that need a clean repo each time.
- Use the constructor for setup; implement `IDisposable.Dispose` for teardown.
- Introduce `IAsyncLifetime` for async setup/teardown (we'll use it once persistence enters the picture).

**NUnit ↔ xUnit notes**

- `[SetUp]` → constructor. `[TearDown]` → `Dispose`. `[OneTimeSetUp]` / `[OneTimeTearDown]` are **not** the equivalents of "static constructor + AppDomain teardown" — those are fixtures (Phase 4).
- The per-test instance is xUnit's strongest opinion: it kills inter-test state leakage by construction. Tests cannot depend on order, ever.
- Why this matters: NUnit tests sharing mutable instance fields will need a re-think — but usually become *clearer* once translated.

**Rider notes**

- Set a breakpoint in the constructor (`F8` to step over, `F7` to step into in IntelliJ keymap), then run two tests in the class together with `Shift+F9`. The breakpoint hits twice with two distinct `this` instances — concrete proof of the per-test lifecycle.
- The **Unit Test Sessions** panel groups by class then method; right-click a method → "Repeat" → "Until Failure" if you suspect a timing or shared-state issue.

**Stretch:** Read the xUnit author's blog post on why `[SetUp]` was rejected; it shapes the rest of the framework's design.

---

## Phase 3 — `[Theory]`, `[InlineData]`, `[MemberData]`, `[ClassData]`  `[x]`

**Goal:** Replace your `[TestCase]` muscle memory and pick the right data source for the job.

- `[InlineData]` for the simple cases: deposit amount → resulting balance.
- `[MemberData]` for cases that need computed or richer data (e.g. `Money` instances, dates, expected ledger states).
- `[ClassData]` and `TheoryData<T1,T2,...>` for strongly-typed test data classes — the cleanest pattern for non-trivial inputs.
- Discuss display name customization and how theory cases appear in the runner.

**NUnit ↔ xUnit notes**

- `[TestCase(...)]` → `[InlineData(...)]` directly.
- `[TestCaseSource]` → `[MemberData]` / `[ClassData]`.
- `[Values]` and `[Combinatorial]` have no direct xUnit equivalent; `TheoryData` + extension methods or third-party libs (e.g. `Xunit.Combinatorial`) fill that gap.
- Theory data must be serializable for the runner to display individual cases — non-serializable inputs collapse to a single test case named generically. We'll hit this and learn to recognize it.

**Rider notes**

- A `[Theory]` appears in the Unit Tests panel as a parent node with one child per `InlineData` case. Each case is independently runnable, debuggable (`Shift+F9` on the case), and re-runnable — much nicer than NUnit's `[TestCaseSource]` collapsing.
- When theory data isn't serializable and Rider shows a single grouped case, that's diagnostic, not a Rider bug — fix the data, not the runner.
- If you don't see your `[MemberData]` source: Rider needs the source member to be `public static`. Common stumble.

**Stretch:** Build a tiny custom `DataAttribute` (we'll do a richer one in Phase 7).

---

## Phase 4 — `IClassFixture<T>` and `ICollectionFixture<T>`  `[x]`

**Goal:** Share expensive setup the xUnit way.

- Build a fixture that pre-seeds an `InMemoryAccountRepository` with a known set of accounts (think: "the standard cast" used by many tests).
- Use `IClassFixture<T>` to share it across tests in one class.
- Promote it to `ICollectionFixture<T>` and a `[CollectionDefinition]` so multiple test classes share one instance.

**NUnit ↔ xUnit notes**

- `[OneTimeSetUp]` → `IClassFixture<T>` (per-class) or `ICollectionFixture<T>` (per-collection).
- `[SetUpFixture]` (assembly-level) → assembly fixtures (xUnit v3) or a collection-fixture pattern with all classes opted-in.
- Fixtures arrive via constructor injection — xUnit doing manual DI before DI was cool.
- A fixture **should be immutable or read-only-ish** by the time tests see it. Mutating shared fixture state across tests is the same hazard NUnit has, but xUnit's parallelism (Phase 8) makes it bite faster.

**Rider notes**

- `Ctrl+B` on `IClassFixture<MyFixture>` to navigate to the fixture; `Alt+F7` ("Find Usages") shows which test classes share it.
- Set a breakpoint in the fixture constructor, then run several tests in the class — it's hit only once, confirming the lifecycle.

**Stretch:** Compare timing of "fixture-shared seeded repo" vs "fresh repo every test" by running with Rider's coverage or profiler — the philosophy starts to feel concrete with numbers attached.

---

## Phase 5 — Output, traits, skipping  `[ ]`

**Goal:** Practical knobs for triage, CI filtering, and conditional execution.

- Inject `ITestOutputHelper` into a constructor; print useful diagnostics from a complex transfer test.
- Add `[Trait("Category", "Integration")]` and filter from CI with `dotnet test --filter`.
- `[Fact(Skip = "...")]` and `Skip.If(...)` (xUnit v3 / `Xunit.SkippableFact`) for runtime-conditional skipping.

**NUnit ↔ xUnit notes**

- `Console.WriteLine` does **not** show in test output — use `ITestOutputHelper`. Frequent stumble.
- `[Category("Integration")]` → `[Trait("Category", "Integration")]`.
- `[Ignore]` → `Skip = "..."`. Conditional skip is more verbose in xUnit but more honest — the test result is "Skipped" with a reason rather than silently passing.

**Rider notes**

- Rider can group the Unit Tests panel by trait: panel → gear icon → **Group By** → **Categories**. This makes `[Trait("Category", "Integration")]` immediately useful, not just a CI knob.
- After a test run, click a test → the output pane shows captured `ITestOutputHelper` output. Output is shown for *every* test in Rider (unlike `dotnet test` defaults), which is occasionally noisy but usually welcome.
- Skipped tests appear with a yellow icon and the skip reason as a tooltip.

**Stretch:** Capture output from a *failing* test under `dotnet test` and notice it prints only on failure by default — a deliberate signal-to-noise choice that differs from Rider's behavior.

---

## Phase 6 — Async tests done right  `[ ]`

**Goal:** Stop fearing async tests; learn the small set of pitfalls.

- Convert `IAccountRepository` to async (`Task<Account> GetAsync(...)`, `Task SaveAsync(...)`).
- Write `async Task` tests; cover `Assert.ThrowsAsync<T>`.
- Show why `async void` tests are silently broken (xUnit warns, but you should *know* why).
- Use `IAsyncLifetime.InitializeAsync` / `DisposeAsync` for an async setup that actually needs to await something (e.g. seeding from a file or a test container).

**NUnit ↔ xUnit notes**

- NUnit and xUnit converged here; the patterns look very similar. The main difference is `IAsyncLifetime` for async setup, which has no clean NUnit equivalent.

**Rider notes**

- Rider's debugger has solid async stack support; in the **Threads & Variables** view, switch to **Async Stack Trace** to see the real chain across `await` points — invaluable when an assertion fails *inside* an awaited call.
- Watch for the orange squiggle Rider draws under `async void` test methods; it's an inspection that catches the mistake before xUnit does. `Alt+Enter` shows the explanation.

**Stretch:** Add a `CancellationToken` parameter to repository methods and write a test that asserts cancellation propagates — easy pattern to skip in real codebases.

---

## Phase 7 — Custom test data and assertions  `[ ]`

**Goal:** Reach for the xUnit extension points only when they earn their keep.

- A custom `DataAttribute` — say, `[FraudScenarioData]` that yields suspicious-transaction sequences from a JSON file in the test project.
- A custom assertion helper for ledger equivalence ("these two ledgers represent the same financial state, ignoring transaction IDs and timestamps") — show why you build a *helper method* instead of subclassing `Assert`.
- Touch `Assert.Equal` with a custom `IEqualityComparer<T>` for finer-grained equality.

**NUnit ↔ xUnit notes**

- NUnit's `IConstraint` system is more elaborate; xUnit deliberately keeps assertions simple and pushes structured equality into your domain layer (`IEquatable<T>`, comparers).
- Custom data attributes are the most useful extension point — they're how you keep `[Theory]` tests readable when the data doesn't fit on one line.

**Rider notes**

- Make sure JSON test data files are marked `Copy if newer` or `Copy always` (right-click → **Properties**, or edit the `.csproj` `<None>` element). Rider's test runner uses the build output, so missing copy settings cause confusing "FileNotFoundException" failures only under tests.

---

## Phase 8 — Parallelism, collections, and ordering  `[ ]`

**Goal:** Understand xUnit's defaults and how to bend them when reality demands.

- Default: tests in different classes run in parallel; tests in the same class run sequentially.
- `[Collection("name")]` — opting multiple classes into the *same* collection makes them serialize relative to each other.
- `xunit.runner.json` — `parallelizeAssembly`, `parallelizeTestCollections`, `maxParallelThreads`.
- When (rarely) test ordering matters: `ITestCaseOrderer` / `ITestCollectionOrderer`. Discuss why "I need ordered tests" is almost always a design smell first.
- Walk through a deliberately flaky test caused by shared state colliding under parallelism, then fix it.

**NUnit ↔ xUnit notes**

- NUnit defaults to **serial**; xUnit defaults to **parallel**. This single difference catches more NUnit-to-xUnit migrations than anything else.
- `[NonParallelizable]` ↔ `[CollectionDefinition("X", DisableParallelization = true)]` plus `[Collection("X")]`.

**Rider notes**

- Rider's unit test runner has its own parallelism setting: **Settings → Build, Execution, Deployment → Unit Testing → Test Runner → "Run tests in parallel."** This is independent of `xunit.runner.json` and applies to how Rider schedules test *processes* — a layer above xUnit's per-collection parallelism. Worth knowing when results differ between Rider and `dotnet test`.
- Right-click a collection node in the Unit Tests panel → **Repeat → Until Failure** is the most reliable way to surface a flaky test caused by parallelism.

**Stretch:** Try `methodDisplay: "method"` vs `"classAndMethod"` in `xunit.runner.json` — small thing, big readability win in CI logs and in Rider's panel.

---

## Phase 9 — Integration testing patterns  `[ ]`

**Goal:** Apply everything to a meatier scenario the way you actually would on a real codebase.

- Wire `TransferService` against a real (in-memory or SQLite) repository inside an `IAsyncLifetime` fixture; run the full happy-path and a couple of failure-path tests.
- If you're interested: introduce `WebApplicationFactory<TEntryPoint>` to host a tiny minimal API around the ledger and write an end-to-end transfer test through HTTP.
- Discuss `Verify` (snapshot testing) — when it's a force multiplier (statements, fraud reports) vs when it's a foot-gun.
- Discuss FsCheck / `Xunit.Combinatorial` for property-based testing of `Money` arithmetic invariants — only if you want to follow that thread.

**NUnit ↔ xUnit notes**

- Both frameworks integrate equally well with `WebApplicationFactory`, Testcontainers, etc. The difference is xUnit's per-test instance + fixture model maps cleanly onto "build the host once per collection, hit it from many tests."

**Rider notes**

- `Verify` integrates with Rider's diff viewer: when a snapshot test fails, the `.received.txt` is diffed against the `.verified.txt` and Rider offers a "Move file" action to accept the new baseline. Once you've used this once, you'll prefer it to the CLI workflow.
- For HTTP tests, Rider's **HTTP Client** scratch files (`.http`) are a useful sidecar for exploring the API by hand while writing the corresponding `WebApplicationFactory` tests.

---

## Phase 10 — Migration & coexistence  `[ ]`

**Goal:** Know enough to migrate a real NUnit suite and to recognize tests that should be *redesigned* rather than *translated*.

- Walk an NUnit test class through a translation — including a `[OneTimeSetUp]` that mutates state, which is the most common "this test is fundamentally telling me something" moment.
- Discuss running NUnit and xUnit in the same solution during a migration (it's allowed; pick conventions).
- Decide what *not* to translate: tests that depend on order, tests that use `[Repeat]` or `[Retry]` (xUnit has no built-in retry — by design — and we'll discuss why and what to do instead).

**Rider notes**

- Rider runs NUnit and xUnit side-by-side without configuration; the Unit Tests panel groups by framework, so you can watch the "NUnit pile shrinks, xUnit pile grows" progress visually during a migration.
- ReSharper / Rider has structural search-and-replace patterns (**Edit → Find → Replace Structurally**, or `Ctrl+Shift+M`) for migrating `[TestCase(1, 2)]` → `[InlineData(1, 2)]` mechanically. We'll write one together if migration is in your future.

---

## Reference: NUnit ↔ xUnit cheat sheet

| NUnit | xUnit |
|---|---|
| `[Test]` | `[Fact]` |
| `[TestCase(1, 2)]` | `[Theory] [InlineData(1, 2)]` |
| `[TestCaseSource]` | `[MemberData]` / `[ClassData]` |
| `[SetUp]` | constructor |
| `[TearDown]` | `IDisposable.Dispose` |
| `[OneTimeSetUp]` | `IClassFixture<T>` constructor |
| `[OneTimeTearDown]` | `IClassFixture<T>` `Dispose` |
| `[SetUpFixture]` | `ICollectionFixture<T>` + `[CollectionDefinition]` |
| `[Category("X")]` | `[Trait("Category", "X")]` |
| `[Ignore("reason")]` | `[Fact(Skip = "reason")]` |
| `Assert.AreEqual(e, a)` | `Assert.Equal(e, a)` |
| `Assert.That(a, Is.EqualTo(e))` | `Assert.Equal(e, a)` (note flipped order vs constraint API) |
| `Assert.Throws<T>(() => ...)` | `Assert.Throws<T>(() => ...)` |
| `Console.WriteLine` | `ITestOutputHelper.WriteLine` |
| `[Parallelizable]` (default off) | parallel by default; opt out per collection |
| `[NonParallelizable]` | `[CollectionDefinition(..., DisableParallelization = true)]` |
| `[Repeat(n)]` | (none — by design) |

---

## Rider IntelliJ-keymap shortcut reference

The shortcuts cited in the phases above, plus a few extras worth memorizing.

| Action | Shortcut |
|---|---|
| Run test/context | `Shift+F10` |
| Debug test/context | `Shift+F9` |
| Run/Debug → choose configuration | `Alt+Shift+F10` / `Alt+Shift+F9` |
| Build run config from caret context | `Ctrl+Shift+F10` |
| Stop running test | `Ctrl+F2` |
| Find Action (search any command) | `Ctrl+Shift+A` |
| Recent files | `Ctrl+E` |
| Go to declaration | `Ctrl+B` (or `Ctrl+Click`) |
| Go to implementation | `Ctrl+Alt+B` |
| Find usages | `Alt+F7` |
| Quick-fix / context actions | `Alt+Enter` |
| Refactor → rename | `Shift+F6` |
| Reformat code | `Ctrl+Alt+L` |
| Optimize imports | `Ctrl+Alt+O` |
| Step over / into / out (debugger) | `F8` / `F7` / `Shift+F8` |
| Resume program | `F9` |

If a shortcut doesn't fire, `Ctrl+Shift+A` ("Find Action") is the fastest way to discover what Rider has actually bound it to under your keymap.

---

## Open questions to revisit

We'll add to this list as we go. Things you've asked about that deserve their own detour:

- _(empty for now)_
