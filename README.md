# Invex.Extensions.Json

Lightweight, allocation-conscious utilities for **flattening, unflattening, and updating
`System.Text.Json` node trees** (`JsonNode` / `JsonObject` / `JsonArray`) using human-readable
path notation such as `user:address:city`.

Ideal for configuration manipulation, ETL pipelines, diffing/patching JSON documents, and
round-tripping JSON to flat key/value stores (e.g., `IConfiguration`-style providers).

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

## Features

- 🔹 **Flatten** any `JsonNode` tree into a flat dictionary of path → value pairs
- 🔹 **Unflatten** flattened pairs back into a hierarchical `JsonObject`
- 🔹 **Replace values** at one or many paths — including inside arrays — without creating structure you didn't ask for
- 🔹 **`JsonObject` extension members** (`ToFlattenedJsonObject`, `ToFlattenedDictionary`, `ToUnflattenedJsonObject`, `HasNestedObjects`) with full type preservation and sparse-array padding
- 🔹 Configurable, multi-character **separators** (default `:`)
- 🔹 No reflection, no regex on hot paths, minimal allocations
- 🔹 Targets `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`

## Installation

```shell
dotnet add package Invex.Extensions.Json
```

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

// Flatten to path/value pairs (bracketed array indices)
IDictionary<string, string?> flat = JsonExtensions.Flatten(json);
// ["user:name"]      = "John"
// ["user:tags:[0]"]  = "admin"
// ["user:tags:[1]"]  = "user"

// Rebuild the tree
JsonObject rebuilt = JsonExtensions.Unflatten(flat);

// Update values in place (bare numeric array indices)
rebuilt.ReplaceValues(new Dictionary<string, string?>
{
    ["user:name"]    = "Jane",
    ["user:tags:1"]  = "editor",
});
```

Or use the type-preserving `JsonObject` extension members:

```csharp
var obj = JsonNode.Parse("""{ "a": { "n": 42, "b": [true, null] } }""")!.AsObject();

JsonObject flatObj = obj.ToFlattenedJsonObject();   // values keep their JSON types
JsonObject roundTrip = flatObj.ToUnflattenedJsonObject();
```

## Documentation

| Topic | Description |
| --- | --- |
| [Getting started](docs/getting-started.md) | Installation, namespaces, and a guided tour |
| [Path notation](docs/path-notation.md) | The two path conventions and when each applies |
| [Flattening & unflattening](docs/flattening.md) | `JsonExtensions.Flatten` / `Unflatten` in depth |
| [JsonObject extension members](docs/jsonobject-extensions.md) | `ToFlattenedJsonObject`, `ToFlattenedDictionary`, `ToUnflattenedJsonObject`, `HasNestedObjects` |
| [Replacing values](docs/replacing-values.md) | `ReplaceValue` / `ReplaceValues` semantics and fallbacks |
| [API reference](api/index.md) | Generated API documentation |

## Choosing the right API

There are two complementary API surfaces — pick based on whether you need **type fidelity**:

| | `JsonExtensions` (static) | `JsonObject` extension members |
| --- | --- | --- |
| Array index notation | Bracketed: `users:[0]:name` | Bare numeric: `users:0:name` |
| Value representation | Strings only | Original JSON types preserved |
| Sparse array indices | Applied in append order | Padded with `null`s |
| Flatten input | Any `JsonNode` | `JsonObject` |
| Best for | Key/value stores, config providers | Lossless round-tripping |

See [Path notation](docs/path-notation.md) for the full comparison.

## License

Licensed under the [MIT License](LICENSE.txt).

