# Copilot Instructions

Guidance for AI agents working in **Invex.Extensions.Json** — a small, focused C# library for
flattening, unflattening, and updating `System.Text.Json` node trees (`JsonNode` / `JsonObject` /
`JsonArray`) using human-readable path notation like `user:address:city`. Keep changes focused
and defer to the linked docs for detail.

## What's in the repo

| Project | Role | Target frameworks |
|---------|------|-------------------|
| `Invex.Extensions.Json` | The library: `JsonExtensions` (`Flatten`, `Unflatten`, `ReplaceValue`, `ReplaceValues`) and `JsonUtil` (`JsonObject` extension members) | `net10.0;net9.0;net8.0;netstandard2.0` |
| `Invex.Extensions.Json.Tests` | NUnit test suite, including a public API surface snapshot test | `net10.0;net9.0;net8.0;net48` |
| `_atom` | Atom build definition (`IBuild.cs`) that generates the GitHub Actions workflows | `net10.0` |

Sources live under `src/`, tests under `tests/`, the Atom build definition under `_atom/`, and the
DocFX documentation site is configured by `docfx.json` with content in `docs/`, `api/`, `index.md`,
and `toc.yml`.

## Build & language specifics

- **.NET 10 SDK** is required (see `global.json`). The library multi-targets down to
  `netstandard2.0`; tests also run on `net48`.
- C# `LangVersion` 14, `ImplicitUsings` and `Nullable` enabled, `TreatWarningsAsErrors` on.
- `JsonUtil` uses **C# 14 `extension` members** (`extension(JsonObject jsonObject) { ... }`) —
  preserve that style when adding members to it.
- Global usings live in each project's `_usings.cs` — add shared usings there, not per-file.
- **Cross-target compatibility**: the `netstandard2.0` build uses the `Polyfill` package and a
  `System.Text.Json` package reference. Use `#if NET8_0_OR_GREATER` guards for newer BCL APIs
  (see the existing `ArgumentNullException.ThrowIfNull` guards) and verify changes compile for
  **all** targets, not just `net10.0`.
- `GenerateDocumentationFile` is on (`CS1591` is in the repo-wide `NoWarn`, so missing docs won't
  fail the build — but by convention every public type and member still gets full XML docs).

Build and test the whole solution:

```shell
dotnet build Invex.Extensions.Json.slnx
dotnet test Invex.Extensions.Json.slnx
```

Build the docs site:

```shell
docfx docfx.json          # add --serve to preview locally
```

## Architecture overview

The entire public surface is two static classes in the `Invex.Extensions.Json` namespace, forming
**two complementary API surfaces** with different path conventions:

- **`JsonExtensions`** — string-valued, works on any `JsonNode`:
  - `Flatten(node, separator)` → `IDictionary<string, string?>` with **bracketed** array indices
    (`users:[0]:name`).
  - `Unflatten(flattened, separator)` → rebuilds the tree; indices applied in append order
    (no null padding); all leaves become JSON strings.
  - `ReplaceValue(root, path, value, separator)` / `ReplaceValues(root, replacements, separator)`
    → in-place updates using **bare numeric** array segments (`users:0:name`).
- **`JsonUtil`** — type-preserving extension members on `JsonObject`, using **bare numeric**
  array segments throughout:
  - `ToFlattenedJsonObject(separator)` — leaf values are deep clones; JSON kinds preserved.
  - `ToFlattenedDictionary(separator)` — stringified values.
  - `ToUnflattenedJsonObject(separator)` — numeric segments become array indices; **sparse
    indices are padded with nulls**.
  - `HasNestedObjects()` — top-level-only flatness check.

### Behavioral contracts (do not break these)

- **The two path conventions are not interchangeable**: `Flatten`/`Unflatten` use `[0]`-style
  indices; everything else uses bare numeric segments. `ReplaceValues` treats `[0]` as an
  ordinary property name.
- **Replacement never creates intermediate containers** — missing objects/arrays and
  out-of-bounds indices leave the document untouched for that entry. Documented fallbacks
  (literal remaining-path property, literal root key) must be preserved.
- **Replacement values are stored as JSON strings** (or JSON null) via `JsonValue.Create`.
- **`ToFlattenedJsonObject`/`ToUnflattenedJsonObject` are lossless** with respect to JSON value
  kinds and never share node references with the source (deep clones).
- **Source objects are never mutated by flatten/unflatten**; only `ReplaceValue`/`ReplaceValues`
  mutate, in place, returning the same instance.
- Separators are strings (multi-character allowed) and default to `":"` everywhere.
- Flatten/unflatten pairs within each API surface must remain exact inverses for well-formed,
  non-empty-container input — change them together, and keep `RoundTripTests` green.

## Key design rules

- Keep the public surface minimal — this library does one thing. Push back on scope creep.
- New behavior should be opt-in via optional parameters with defaults that preserve existing
  behavior.
- No reflection, no regex on hot paths; flattening uses a single reusable `StringBuilder` —
  keep new code similarly allocation-conscious.

## Atom workflows

The GitHub Actions workflow YAML under `.github/workflows/` (`Validate.yml`, `Build.yml`,
`Dependabot Enable auto-merge.yml`, `Cleanup Prereleases.yml`) is **generated** from the Atom
build definition in `_atom/IBuild.cs`.

Whenever you change anything that affects the workflows — targets, workflow definitions, triggers,
options, or params/secrets — regenerate the YAML:

```shell
atom gen
```

(equivalently `dotnet run --project _atom -- gen`). Commit the regenerated `.github/workflows/`
files alongside your `_atom/` changes; never hand-edit the generated YAML.

A drift between `_atom/IBuild.cs` and the committed YAML should be treated as a missing
`atom gen` run.

## Conventions

- Annotate every new public type and member with `[PublicAPI]` — the in-repo analyzer flags
  anything missing, and warnings are errors.
- Add XML doc comments to all public types and members. Match the existing `<summary>` /
  `<param>` / `<returns>` / `<remarks>` / `<example>` style — including proper
  `<list type="bullet">` and `<para>` formatting — and keep docs **accurate to the
  implementation** (e.g. exact fallback behavior, padding vs. append-order semantics).
- Use Conventional Commits — the prefix drives versioning (see `GitVersion.yml`):

  | Prefix | Version bump |
  |--------|--------------|
  | `breaking:` / `major:` | Major |
  | `feat:` / `feature:` / `minor:` | Minor |
  | `fix:` / `patch:` | Patch |
  | `semver-none` / `semver-skip` | No bump |

- When adding user-facing features, update the relevant `docs/` page and `README.md`. The README
  is packed into the NuGet package — keep links absolute where they must work outside the repo.

## Testing & the Verify workflow

- Tests use **NUnit** with **Shouldly**, **FakeItEasy**, and **Verify** (`Verify.NUnit`) for
  snapshot/approval testing. Behavioral tests live under
  `tests/Invex.Extensions.Json.Tests/JsonExtensionsTests/` (one file per feature:
  `FlattenTests`, `UnflattenTests`, `ReplaceTests`, `ReplaceManyTests`, `RoundTripTests`) plus
  `JsonUtilTests.cs`.
- A snapshot test fails when its output differs from the committed `*.verified.txt`. On failure,
  Verify writes a `*.received.txt` next to it.
- If the diff is unintended, fix the code. If the change is valid (expected new output), accept
  it and re-run:
  1. Overwrite the `*.verified.txt` with the contents of the matching `*.received.txt`.
  2. Delete the `*.received.txt`.
  3. Re-run `dotnet test` to confirm the suite is green.
- `PublicApiTests.VerifyPublicApiSurface.verified.txt` tracks the **complete public API**. An
  unexpected diff there signals an unintentional API change — treat it as such and double-check
  before accepting. The Validate workflow's `CheckPrForBreakingChanges` target inspects changes
  to `tests/**/*.verified.txt` on PRs, so API-surface changes must be intentional and committed.

## Adding a new public method

1. Decide which surface it belongs to: `JsonExtensions` (string-valued, any `JsonNode`) or
   `JsonUtil` (type-preserving `JsonObject` extension members) — and use that surface's path
   convention consistently.
2. Accept a `string separator = ":"` parameter if the method deals with paths.
3. Guard arguments with the existing `#if NET8_0_OR_GREATER` / `ThrowIfNull` pattern and verify
   the `netstandard2.0` and `net48` builds.
4. Annotate with `[PublicAPI]` and write full XML docs (lists, paras, examples).
5. Add unit tests (including round-trip tests where applicable) and update
   `PublicApiTests.VerifyPublicApiSurface.verified.txt` (see the Verify workflow above).
6. Document it in the relevant `docs/` page and, if user-facing, the README.

## Defer to the docs

For anything beyond the above, prefer these over duplicating detail:

- `README.md` — package overview, quick start, and API-selection guidance.
- `docs/getting-started.md` — installation, namespaces, and a guided tour.
- `docs/path-notation.md` — the two path conventions, separators, and edge cases.
- `docs/flattening.md` — `JsonExtensions.Flatten` / `Unflatten` in depth.
- `docs/jsonobject-extensions.md` — the type-preserving `JsonUtil` members.
- `docs/replacing-values.md` — `ReplaceValue` / `ReplaceValues` semantics and fallbacks.
- `api/index.md` — API reference landing page and local DocFX usage.

