# Phase 0 — Project setup & first run

> The high-level summary of this phase lives in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This file is the detailed walkthrough.

## How to use this file

- Each step header ends with `` `[ ]` ``. Change to `` `[x]` `` when you've completed that step.
- If you have to step away mid-tutorial, the **first unchecked step in order** is where you resume.
- The "Notes & questions" section at the bottom is yours — questions, surprises, things you want to circle back to.

## Decisions made

- **xUnit version:** v3 (chosen 2026-05-09).
  - Considered: v2 (matches most existing code/blogs), v3 (current dev focus, cleaner API), or both side-by-side.
  - Rationale: learn what we'll actually use going forward; accept slightly more setup work.
- **Solution name:** `LearnXunit` (PascalCase to match C# conventions).
- **Layout:** `src/Ledger` for the production class library, `tests/Ledger.Tests` for the test project. Modern .NET convention; worth getting into the habit.
- **Rider usage: Path D — Remote Development via JetBrains Gateway** (switched 2026-05-15, superseding the earlier "editor only" decision once the SDK mismatch was no longer relevant).
  - **Setup:** JetBrains Gateway on Windows connects to a Rider backend running inside WSL2 Ubuntu (using the Ubuntu .NET 10 SDK). A thin client on Windows is the UI; build, edit, debug, and run all execute Linux-side. The Rider backend lives at `~/.cache/JetBrains/RemoteDev/dist/...`.
  - **What works:** build, edit, navigate, refactor, debug, and test execution via the auto-generated **`Ledger.Tests` Run configuration** (Rider creates this automatically because `<OutputType>Exe</OutputType>` makes the test project a runnable executable; running it invokes the MTP test runner directly with rich console output).
  - **What does NOT work:** Rider's Test Explorer doesn't populate; `[Fact]` methods don't get gutter icons. Verified after trying `<IsTestProject>true</...>`, the "Enable Testing Platform support" toggle (Settings → Build, Execution, Deployment → Unit Testing → Testing Platform), cache invalidation, and plugin updates — none fixed discovery. Likely a Rider 2026.1.1R + xUnit v3 + MTP bug. Worth watching JetBrains' YouTrack for fixes.
  - **Implication:** for "run all tests," use the Run configuration in Rider. For per-test runs, use CLI: `dotnet test --filter "..."`. Rider notes elsewhere in the tutorial about gutter-icon / Test Explorer workflows don't apply; editor, refactor, and debug-via-Run-config notes still do.
  - **Why Path D over editor-only:** Path D eliminates the cross-WSL/Windows boundary issues entirely (everything is Linux-side) while keeping the personal GitHub identity / backup-scope boundary intact (code still lives in WSL).

- **.NET 10 SDK and TFM (2026-05-12).** Bumped both `.csproj` files from `net8.0` to `net10.0`. Added `global.json` at the repo root pinning `sdk.version: "10.0.107"` with `rollForward: "latestFeature"`, and setting `test.runner: "Microsoft.Testing.Platform"` to opt `dotnet test` into MTP-native execution on .NET 10.
  - Triggers: (a) .NET 8 LTS expires ~Nov 2026 — short runway; (b) the `dotnet test` console-output gap on .NET 8 + MTP (rich failure detail going to a log file instead of stdout) is closed in .NET 10's `dotnet test` rework. Both verified post-upgrade.
  - Install path: Canonical's Ubuntu archive (`sudo apt install dotnet-sdk-10.0`), not Microsoft's apt repo. Ubuntu 24.04 ships .NET 10 in its own feed, so the single-vendor native path was available. Both SDKs (8.0.126 and 10.0.107) coexist at `/usr/lib/dotnet/sdk`; `global.json` is what pins this project to .NET 10.

## Goal

Get a working solution with a passing test and a deliberately failing test. Understand the runner ergonomics in both `dotnet test` (CLI) and Rider, and read enough of the test project's `.csproj` to recognize the package surface area.

---

## Steps

### Step 1 — Install the xUnit v3 templates  `[x]`

```
dotnet new install xunit.v3.templates
```

Adds `xunit3` (and friends like `xunit3-extension`) to the `dotnet new` template list. One-time install per machine. Verify with:

```
dotnet new list xunit
```

You should now see `xunit3` alongside the v2 `xunit` template.

### Step 2 — Create the solution  `[x]`

From the repo root (`/home/larryjones/professional/projects/learn-xunit`):

```
dotnet new sln -n LearnXunit
```

The repo is the *learning environment*; the *domain under test* is the Ledger. Naming choice is yours — adjust later commands if you change it.

### Step 3 — Create the production project  `[x]`

```
dotnet new classlib -n Ledger -o src/Ledger
```

`-o src/Ledger` puts it in a `src/` subfolder. Delete the auto-generated `Class1.cs` after it runs — real types come in later phases.

### Step 4 — Create the test project (v3)  `[x]`

```
dotnet new xunit3 -o tests/Ledger.Tests
```

`-n` is omitted: `dotnet new` uses the output directory's leaf name as the project name, giving `Ledger.Tests`.

**Before doing anything else, open `tests/Ledger.Tests/Ledger.Tests.csproj` in Rider and read it.** The xUnit v3 template defaults to the **Microsoft Testing Platform (MTP)** rather than the older VSTest host model, which keeps the file remarkably small.

Things you should find:

- **`<PackageReference Include="xunit.v3.mtp-v2" />`** — the only package reference. A *meta-package* that transitively brings in `xunit.v3` itself, the in-process MTP runner, and the MSBuild integration that makes everything work. The `-v2` refers to **Microsoft Testing Platform v2** (the second-generation MTP integration shape), **not** xUnit v2.
- **`<Using Include="Xunit" />`** — an MSBuild *implicit using*; the compiler injects `using Xunit;` into every `.cs` file in this project so you never have to write it.
- **`<Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />`** — and a corresponding `xunit.runner.json` file in the project. Runner configuration (parallelism, display name format, timeout); we edit it in Phase 8.
- **`<TestingPlatformDotnetTestSupport>true</...>`** in the `<PropertyGroup>` — the property that tells the build system "let `dotnet test` invoke this project as an MTP test executable." (You may have expected `<UseMicrosoftTestingPlatform>` instead; the meta-package handles MTP enablement implicitly via its MSBuild props, so the `TestingPlatformDotnetTestSupport` property is what's left for you to control.)
- **`<OutputType>Exe</OutputType>`** — either present explicitly in the `<PropertyGroup>` or contributed implicitly by the meta-package. Under MTP, test projects are *executables*: the project hosts its own runner.

Things you will **not** find (and which would appear in a v2 / VSTest test project):

- `Microsoft.NET.Test.Sdk` — the generic VSTest host process. Not needed under MTP; the host *is* your project.
- `xunit.runner.visualstudio` — the VSTest adapter for xUnit. Not needed because there is no VSTest host to adapt to. (The "Visual Studio" branding on this package is historical; in v2 setups it's the adapter every VSTest-based runner uses, including Rider, VS Code, Visual Studio, and `dotnet test`.)

#### A note on the host model

Worth holding this distinction in your head; it shapes a lot of what follows in xUnit v3.

- **VSTest model** (xUnit v2, NUnit, classic MSTest): test project is a **class library**. `Microsoft.NET.Test.Sdk` provides a generic host process. An adapter (`xunit.runner.visualstudio`, `NUnit3TestAdapter`, etc.) bridges the framework to that host. `dotnet test` orchestrates.
- **MTP model** (xUnit v3 default): test project is an **executable** that embeds its own runner. No separate host. `dotnet test` simply runs the executable. You can also run it directly: `dotnet run --project tests/Ledger.Tests`. Try this once after Step 6 — it's the moment MTP "clicks."

This is the cleanest moment in the whole tutorial to see the package surface area; once we add more, it gets noisier.

### Step 5 — Wire everything together  `[x]`

Three commands; in this order:

```
dotnet sln add src/Ledger/Ledger.csproj
dotnet sln add tests/Ledger.Tests/Ledger.Tests.csproj
dotnet add tests/Ledger.Tests/Ledger.Tests.csproj reference src/Ledger/Ledger.csproj
```

The first two register both projects in the solution file (so Rider sees them when opening the `.sln`). The third makes `Ledger.Tests` reference `Ledger`, so test code can `using Ledger;` and see production types.

### Step 6 — First run  `[x]`

```
dotnet test
```

The v3 template scaffolds one or two passing sample tests (likely in `UnitTest1.cs`). You should see a green run with the test passing.

**Then try invoking the test project directly:**

```
dotnet run --project tests/Ledger.Tests
```

Same tests, same outcome — but invoked as the **MTP test executable** rather than via the `dotnet test` orchestrator. Output formatting will be slightly different. This is the host-model distinction from Step 4 made concrete: the test project *is* the runner.

**Optional, but worth knowing now:** `dotnet watch test` runs the suite continuously, re-executing on every file save. Run it in a second terminal pane while you edit for a tight feedback loop — the closest CLI equivalent to a live test runner. Useful from Phase 1 onward.

### Step 7 — Make it fail  `[x]`

In the sample test file, change the assertion so the test fails — for example, `Assert.True(true)` → `Assert.True(false, "this is intentional")`. Run it:

```
dotnet test
echo $?
```

What to notice:

- The exception type (`Xunit.Sdk.TrueException` or similar) and how the message is formatted. xUnit failure messages read **"Expected ... Actual ..."** — that ordering will matter when we hit argument-order pitfalls in Phase 1.
- The location info in the failure output — file path + line number. Most modern terminals make that clickable; Rider opens to the line if you click through.
- The exit code from `echo $?` is **non-zero** on test failure (typically `1`). This is the contract CI relies on; the exit code is how `dotnet test` becomes a build gate.

Then revert the assertion so the test passes again, and confirm `echo $?` returns `0`.

---

## Notes & questions

_Fill this section in as you go. Things that surprised you, things that made you stop and think, links to docs you ended up reading. This is the part of the file that becomes most valuable on a refresher visit months from now._

### Missing json file warningf

> **Update 2026-05-12:** `global.json` now exists at the repo root, created during the .NET 10 SDK upgrade (see "Decisions made" above). It pins the SDK and opts `dotnet test` into MTP. The post-action warning below would no longer fire today.

FYI. When I created the tests project, `tests/LedgerTests`, in Step 4, I encountered the following warning:

> The template "xUnit.net v3 Test Project" was created successfully.
> 
> Processing post-creation actions...
> Restoring /home/larryjones/professional/projects/learn-xunit/tests/LedgerTests/LedgerTests.csproj:
>  Determining projects to restore...
>  Restored /home/larryjones/professional/projects/learn-xunit/tests/LedgerTests/LedgerTests.csproj (in 2.37 sec).
> Restore succeeded.
> 
> Unable to find the json file in the solution
> Post action failed.
> Manual instructions: If using .NET 10 SDK or later, manually update or create 'global.json' as per https://xunit.net/docs/getting-started/v3/microsoft-testing-platform#using-net-sdk-version-10-or-later 

When I asked Claude about this warning, here was the response:

> ● That warning is benign on .NET 8 — your project is correctly created and ready to use. Let me unpack
>    what triggered it.
> 
>   What the warning is about. xUnit v3 ships on top of the Microsoft Testing Platform (MTP) — a newer
>   testing host than the older VSTest one. The xUnit v3 template includes a post-creation action that
>   wants to ensure your global.json is set up correctly for MTP. Specifically, on .NET 10 SDK and
>   later, MTP requires either a global.json setting or an explicit opt-in to VSTest, because the .NET
>   10 CLI has built-in test-runner integration that conflicts otherwise.
> 
>   You're on .NET 8, so none of that applies — but the post-action looks for global.json regardless and
>    complains when it can't find one in your solution. Project files: created correctly. NuGet restore:
>    succeeded. The post-action that failed is a precautionary thing for a future SDK you're not using
>   yet.
> 
>   What global.json is (since you may not have hit it before): a JSON file at a solution/repo root that
>    pins the .NET SDK version and configures rollforward behavior. Useful when you want dotnet build to
>    fail loudly if someone tries to build on the wrong SDK. We don't need one for this tutorial.
> 
>   Verdict: ignore the warning. Mark Step 4 done.

#### No color when all tests passed.

I observed that passing tests contain **no** green output. I thought I might need to set a switch to produce green output; however, after checking many, many, different details with Claude, Claude concluded that passing tests **do not** print in green; however, failing tests do print in red.

Irritating, but useful to know.

Claude recommended scanning the text to look for `Failed: 0`. Sigh..

> **Update 2026-05-12 (post-.NET-10 upgrade):** The `dotnet test` output format changed substantially — passing runs now print `Test run summary: Passed!` with a small block of `total/failed/succeeded/skipped/duration` lines, rather than the single `Failed: 0, Passed: 1, ...` line we used to scan. Whether the new format actually renders `Passed!` in green is worth re-checking the next time you see a passing run; the structural change makes that question worth revisiting rather than carrying the .NET 8 observation forward as a settled fact.

#### Preferred unit test commands

> **Update 2026-05-12 (post-.NET-10 upgrade):** This guidance is now historical. On .NET 10 with `global.json` opting into `test.runner: "Microsoft.Testing.Platform"`, `dotnet test` invokes MTP natively and prints rich per-failure detail (custom message + file:line + partial stack trace) directly to the console. It's the canonical command again. `dotnet run --project tests/<TestProject>` still works and is useful pedagogically — it makes the MTP host-model point concrete (the test project *is* an executable) — but it's no longer required as a workaround for an output gap.
>
> The original guidance below applied to the .NET 8 + MTP era and is preserved for context.

Although the command `dotnet test --tl` works and displays failing output in red, the layout seems to run together.

The preferred way to view output is the more complicated command, `dotnet run --project tests/Ledger.Tests`. If a test fails, this prints a failure message in red and a (partial) stacktrace. If all tests pass, it prints everything in white (sigh).

-
