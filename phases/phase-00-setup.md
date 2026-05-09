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

### Step 4 — Create the test project (v3)  `[ ]`

```
dotnet new xunit3 -o tests/Ledger.Tests
```

`-n` is omitted: `dotnet new` uses the output directory's leaf name as the project name, giving `Ledger.Tests`.

**Before doing anything else, open `tests/Ledger.Tests/Ledger.Tests.csproj` in Rider and read it.** Look for:

- The `<PackageReference>` entries — note that v3 uses `xunit.v3` (not `xunit`) as the package name. Compare to v2, which uses `xunit`.
- The `xunit.runner.visualstudio` reference — this is what makes `dotnet test` and Rider's runner discover xUnit tests. Without it, your tests exist but nothing finds them.
- The `Microsoft.NET.Test.Sdk` reference — the generic test host. Required for any test framework, not xUnit-specific.

This is the cleanest moment in the whole tutorial to see the package surface area; once we add more, it gets noisier.

### Step 5 — Wire everything together  `[ ]`

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

The v3 template scaffolds one or two passing sample tests (likely in `UnitTest1.cs`). You should see a green run with the test passing. Then **open the solution in Rider** and run the same test from the gutter icon (`Shift+F10` with the caret on the test method, IntelliJ keymap).

Compare the two surfaces:

- **CLI:** one-line summary, output buffered until the run ends.
- **Rider:** tree view, per-test timing, captured output panel, ability to re-run a single case.

That difference is exactly what makes Rider's runner pleasant once we get to `[Theory]` cases in Phase 3.

### Step 7 — Make it fail  `[ ]`

In the sample test file, change the assertion so the test fails — for example, `Assert.True(true)` → `Assert.True(false, "this is intentional")`. Run again from both surfaces.

What to notice:

- The exception type (`Xunit.Sdk.TrueException` or similar) and how the message is formatted.
- Rider's failure output and whether it offers clickable navigation back to the failing line.
- The exit code of `dotnet test` — run `echo $?` right after; non-zero on failure. This is what CI looks at.

Then revert the assertion so the test passes again.

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
