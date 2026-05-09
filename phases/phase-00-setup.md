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
- **Rider usage: editor only.** All builds and test runs go through `dotnet` from the Ubuntu CLI. Rider is used for code intelligence, navigation, refactoring, and reading source — not for running or debugging tests.
  - Reason: project lives in WSL2 Ubuntu (separate personal GitHub identity, and keeping personal projects out of the Windows-side backup scope); Rider runs natively on Windows. Crossing that boundary for build/run produces brittle errors (targeting-pack mismatches on the Windows side, file-permission issues across the WSL ↔ Windows seam).
  - Implication: skip Rider notes elsewhere in the tutorial that depend on the test-runner UI or the Rider debugger. Navigation/editor Rider notes still apply.

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

### Step 6 — First run  `[ ]`

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

### Step 7 — Make it fail  `[ ]`

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

-
