# Agent Instructions

Guidance for AI agents working in **Invex.Extensions.Json**. Keep changes
focused and defer consumer-facing usage details to `README.md` and `docs/`.

## Repository map

| Path | Purpose |
| --- | --- |
| `src/Invex.Extensions.Json/` | Library source |
| `tests/Invex.Extensions.Json.Tests/` | NUnit tests and Verify snapshots |
| `_atom/` | Atom build definition |
| `docs/`, `api/`, `docfx.json` | DocFX documentation |
| `.github/workflows/` | Generated GitHub Actions workflows |
| `Invex.Extensions.Json.slnx` | Solution used for local builds and tests |

The library exposes two public surfaces in the `Invex.Extensions.Json`
namespace:

- `JsonExtensions` works on any `JsonNode` and uses string values.
- `JsonUtil` supplies C# 14 extension members on `JsonObject` and preserves
  JSON value types when using `ToFlattenedJsonObject`.

## Build, test, and documentation commands

The repository requires the .NET SDK selected by `global.json` (`10.0.0`,
rolling forward to later major SDKs). Run commands from the repository root:

```shell
dotnet build Invex.Extensions.Json.slnx
dotnet test Invex.Extensions.Json.slnx
docfx docfx.json
docfx docfx.json --serve
```

The library targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.
The test project targets `net10.0`, `net9.0`, `net8.0`, and `net48`.
Validate changes against all relevant targets, not only the current runtime.

After code changes, run ReSharper cleanup over the solution's C# files:

```powershell
$sdk = dotnet --version
jb cleanupcode Invex.Extensions.Json.slnx --include="**.cs" --toolset-path="C:\Program Files\dotnet\sdk\$sdk\MSBuild.dll"
```

`--toolset-path` is required. Without it, `jb` can select Visual Studio
BuildTools MSBuild and fail with `MSB4236` while resolving
`Microsoft.NET.SDK.WorkloadAutoImportPropsLocator`. If `jb` is unavailable,
install it with:

```powershell
dotnet tool install -g JetBrains.ReSharper.GlobalTools
```

Cleanup honors `.editorconfig` and shared `*.DotSettings`; no extra flags are
needed.

## Build and language conventions

- `ImplicitUsings`, nullable analysis, XML documentation generation, and
  `TreatWarningsAsErrors` are enabled in `Directory.Build.props`.
- Global usings belong in each project's `_usings.cs`, not in individual files.
- `JsonUtil` uses C# 14 `extension(JsonObject jsonObject)` members; preserve
  that style when adding members.
- The `netstandard2.0` target uses `Polyfill` and a `System.Text.Json` package.
  Guard newer BCL APIs with the existing `#if NET8_0_OR_GREATER` pattern.
- Every new public type and member needs `[PublicAPI]`, where the public API
  analyzer applies, and complete XML documentation by convention.
- Keep public API minimal. Internal cross-member helpers should remain
  `internal` or `private` unless there is a deliberate API change.
- Prefer allocation-conscious traversal and existing helpers. Do not add
  reflection or regular expressions to hot paths.

## JSON path contracts

Do not merge the two path conventions:

| Surface | Array syntax | Value behavior |
| --- | --- | --- |
| `JsonExtensions.Flatten` / `Unflatten` | Bracketed, `items:[0]` | String values; unflattened leaves become JSON strings |
| `JsonUtil` flatten/unflatten members | Bare numeric, `items:0` | JSON kinds preserved by `ToFlattenedJsonObject` |
| `ReplaceValues` | Bare numeric, `items:0` | In-place string or JSON-null replacements |

Additional contracts:

- Separators default to `:` and may be multi-character.
- Flattening and unflattening create new results and do not mutate sources.
- Empty containers produce no flattened entries and are not recoverable from a
  flat representation.
- `JsonExtensions.Unflatten` applies bracketed indices in append order and
  does not sparse-pad arrays.
- `JsonUtil.ToUnflattenedJsonObject` treats numeric segments as array indices
  and pads sparse arrays with nulls.
- `ToFlattenedJsonObject` and `ToUnflattenedJsonObject` deep-clone leaf nodes.
- `ReplaceValue` and `ReplaceValues` mutate and return the same root object.
- Replacement never creates intermediate containers. Preserve the existing
  literal-path fallbacks and out-of-bounds behavior.
- `ReplaceValues` treats `[0]` as a property name, not an array index.

When changing flattening or unflattening, update the corresponding tests and
keep the round-trip behavior for supported, non-empty input intact.

## Atom workflows

The YAML under `.github/workflows/` and `.github/dependabot.yml` is generated
from `_atom/IBuild.cs`. If a change affects workflow targets, triggers,
matrices, options, parameters, secrets, or Dependabot configuration, regenerate
the files:

```shell
atom gen
```

Equivalent command:

```shell
dotnet run --project _atom -- gen
```

Never hand-edit generated workflow files. Commit regenerated output with the
Atom source change and treat source/output drift as a missed generation step.

## Testing and Verify snapshots

Tests use NUnit, Shouldly, FakeItEasy, and Verify (`Verify.NUnit`). Behavioral
tests are under `tests/Invex.Extensions.Json.Tests/JsonExtensionsTests/`;
`JsonUtilTests.cs` covers the `JsonObject` surface, and
`PublicApiTests.cs` verifies the public API snapshot.

When a Verify test fails, inspect the matching `*.received.txt`:

1. Fix the implementation if the output is unintended.
2. If the output is intentional, replace the matching `*.verified.txt` with
   the received content.
3. Delete the `*.received.txt` file.
4. Run `dotnet test Invex.Extensions.Json.slnx` again.

Keep verified files LF-only. Changes to
`PublicApiTests.VerifyPublicApiSurface.verified.txt` must be intentional because
PR validation checks verified files for breaking API changes.

## Versioning, documentation, and change checklist

Use Conventional Commits; `GitVersion.yml` maps prefixes as follows:

| Prefix | Version bump |
| --- | --- |
| `breaking:`, `major:` | Major |
| `feat:`, `feature:`, `minor:` | Minor |
| `fix:`, `patch:` | Patch |
| `semver-none`, `semver-skip` | No bump |

For a code change:

1. Follow existing patterns and keep the edit surgical.
2. Add tests, XML docs, and `[PublicAPI]` for new public members.
3. Build and test all relevant target frameworks.
4. Run ReSharper cleanup and include intentional formatting changes.
5. Update `README.md` or the relevant `docs/` page for user-facing behavior.
6. Regenerate Atom outputs when workflow inputs or definitions change.
