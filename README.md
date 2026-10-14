# Invex.Extensions.Json

[![Build](https://github.com/Invex-Games/json-extensions/actions/workflows/Build.yml/badge.svg)](https://github.com/Invex-Games/json-extensions/actions/workflows/Build.yml)
[![NuGet](https://img.shields.io/nuget/v/Invex.Extensions.Json.svg)](https://www.nuget.org/packages/Invex.Extensions.Json)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/Invex-Games/json-extensions/blob/main/LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0%20%7C%20Standard%202.0-512BD4.svg)](https://dotnet.microsoft.com/)

`Invex.Extensions.Json` is a small, allocation-conscious library for flattening,
unflattening, and updating `System.Text.Json` node trees
(`JsonNode`, `JsonObject`, and `JsonArray`) with readable paths such as
`user:address:city`. It is useful for configuration and environment-variable
adapters, ETL pipelines, JSON diffing or patching, and exchanging JSON with flat
key/value stores.

## Features

- **Flatten any node.** Turn any `JsonNode` into path/value pairs.
- **Rebuild hierarchies.** Recreate a `JsonObject` from flattened pairs.
- **Update in place.** Replace one or many values in an existing `JsonObject`, including array elements.
- **Type preservation.** Keep JSON value types when flattening to a `JsonObject`.
- **Configurable separators.** Use any separator, including multi-character ones such as `"__"`.
- **Lean hot paths.** No reflection or regular expressions when flattening and unflattening.

## Installation

```shell
dotnet add package Invex.Extensions.Json
```

The package targets `net10.0` and `netstandard2.0`, and
exposes the `Invex.Extensions.Json` namespace. The `netstandard2.0` target
includes a `System.Text.Json` package dependency.

## Quick start

```csharp
using System.Text.Json.Nodes;
using Invex.Extensions.Json;

var json = JsonNode.Parse("""
{
  "user": {
    "name": "John",
    "tags": ["admin", "user"]
  }
}
""")!;

// String-valued flattening uses bracketed array indices.
IDictionary<string, string?> flat = JsonExtensions.Flatten(json);
// user:name       = "John"
// user:tags:[0]   = "admin"
// user:tags:[1]   = "user"

// The matching operation rebuilds the hierarchy.
JsonObject rebuilt = JsonExtensions.Unflatten(flat);

// Replacements mutate the existing object and use bare numeric indices.
rebuilt.ReplaceValues(new Dictionary<string, string?>
{
    ["user:name"] = "Jane",
    ["user:tags:1"] = "editor",
});
```

## Usage

### Choose an API surface

The library has two complementary path and value models. Pair operations from
the same column when round-tripping.

| | `JsonExtensions` | `JsonObject` extension members |
| --- | --- | --- |
| Flattening input | Any `JsonNode` | `JsonObject` |
| Array path syntax | Bracketed: `users:[0]:name` | Bare numeric: `users:0:name` |
| Flattened values | `string?` | Original JSON nodes, or `string?` for the dictionary method |
| Unflattened arrays | Append order; no sparse padding | Numeric indices; sparse indices padded with `null` |
| Best suited to | Configuration and string key/value stores | Lossless JSON round-tripping |

See [Introduction](https://github.com/Invex-Games/json-extensions/blob/main/docs/introduction.md)
for when to use each surface.

### Flatten for a key/value store

`JsonExtensions.Flatten` returns string values (JSON null becomes a null value),
and `Unflatten` rebuilds a `JsonObject` whose leaves are all JSON strings. Use
the same separator for both halves of a round trip:

```csharp
var flat = JsonExtensions.Flatten(json, separator: "__");
var rebuilt = JsonExtensions.Unflatten(flat, separator: "__");
```

Property names are not escaped, so pick a separator that never occurs in them.
See [Flattening and Unflattening](https://github.com/Invex-Games/json-extensions/blob/main/docs/flattening.md)
and [Path Notation](https://github.com/Invex-Games/json-extensions/blob/main/docs/path-notation.md).

### Round-trip without losing value types

```csharp
var source = JsonNode.Parse(
    """{ "a": { "number": 42, "enabled": true, "items": [null, "x"] } }""")!
    .AsObject();

JsonObject flatObject = source.ToFlattenedJsonObject();
// a:number = 42, a:enabled = true, a:items:0 = null, a:items:1 = "x"

JsonObject roundTrip = flatObject.ToUnflattenedJsonObject();
Dictionary<string, string?> flatDictionary = source.ToFlattenedDictionary();
bool hasNestedValues = source.HasNestedObjects();
```

Leaf nodes are deep-cloned in both directions, so results never share mutable
nodes with their source. See
[JsonObject Extension Members](https://github.com/Invex-Games/json-extensions/blob/main/docs/jsonobject-extensions.md).

### Replace values in place

`ReplaceValue` and `ReplaceValues` modify the supplied `JsonObject` and return
the same instance. Replacement values are stored as JSON strings (or JSON null),
and no intermediate objects or arrays are ever created. `ReplaceValues` can step
into existing arrays with bare numeric segments such as `users:0:name`; a
bracketed `[0]` is treated as a literal property name.

```csharp
var obj = JsonNode.Parse("""
{ "user": { "address": { "city": "NYC" } }, "users": [ { "name": "A" } ] }
""")!.AsObject();

obj.ReplaceValue("user:address:city", "LA")
   .ReplaceValues(new Dictionary<string, string?> { ["users:0:name"] = "Alpha" });
```

See [Replacing Values](https://github.com/Invex-Games/json-extensions/blob/main/docs/replacing-values.md)
for the complete traversal and fallback rules.

## Documentation

Full documentation: [https://invex-games.github.io/json-extensions/](https://invex-games.github.io/json-extensions/)

| Guide | Contents |
| --- | --- |
| [Introduction](https://github.com/Invex-Games/json-extensions/blob/main/docs/introduction.md) | What the library is for, the two API surfaces, and design principles |
| [Getting Started](https://github.com/Invex-Games/json-extensions/blob/main/docs/getting-started.md) | Installation, namespaces, and guided examples |
| [Path Notation](https://github.com/Invex-Games/json-extensions/blob/main/docs/path-notation.md) | Array conventions, separators, values, and edge cases |
| [Flattening and Unflattening](https://github.com/Invex-Games/json-extensions/blob/main/docs/flattening.md) | `JsonExtensions.Flatten` and `Unflatten` |
| [JsonObject Extension Members](https://github.com/Invex-Games/json-extensions/blob/main/docs/jsonobject-extensions.md) | Type-preserving flattening and reconstruction |
| [Replacing Values](https://github.com/Invex-Games/json-extensions/blob/main/docs/replacing-values.md) | In-place updates, arrays, and fallbacks |
| [API Reference](https://invex-games.github.io/json-extensions/api/index.html) | Generated reference for every public type |

## Development

The repository builds with the .NET SDK selected by `global.json`. From the
repository root:

```shell
dotnet build Invex.Extensions.Json.slnx
dotnet test Invex.Extensions.Json.slnx
```

The documentation site is built with [DocFX](https://dotnet.github.io/docfx/)
from the conceptual pages in `docs/` and the XML doc comments in the source.
Build or preview it through the Atom build:

```shell
dotnet run --project _atom -- BuildDocs
dotnet run --project _atom -- ServeDocs
```

(`atom BuildDocs` and `atom ServeDocs` when the Atom tool is installed.) To run
DocFX directly instead:

```shell
dotnet tool install -g docfx
docfx docfx.json --serve
```

Then browse to `http://localhost:8080`.

## License

Licensed under the [MIT License](https://github.com/Invex-Games/json-extensions/blob/main/LICENSE.txt).
