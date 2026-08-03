# Invex.Extensions.Json

`Invex.Extensions.Json` is a small, allocation-conscious library for flattening,
unflattening, and updating `System.Text.Json` node trees
(`JsonNode`, `JsonObject`, and `JsonArray`) with readable paths such as
`user:address:city`.

It is useful for configuration and environment-variable adapters, ETL pipelines,
JSON diffing or patching, and exchanging JSON with flat key/value stores.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

## Features

- Flatten any `JsonNode` into path/value pairs.
- Rebuild a `JsonObject` from flattened pairs.
- Replace one or many values in an existing `JsonObject`, including array elements.
- Preserve JSON value types when flattening to a `JsonObject`.
- Use configurable separators, including multi-character separators.
- Avoid reflection and regular expressions on the hot paths.
- Target `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`.

## Installation

```shell
dotnet add package Invex.Extensions.Json
```

The package exposes the `Invex.Extensions.Json` namespace. The `netstandard2.0`
target includes a `System.Text.Json` package dependency.

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

## Choose an API surface

The library has two complementary path and value models. Pair operations from
the same column when round-tripping.

| | `JsonExtensions` | `JsonObject` extension members |
| --- | --- | --- |
| Flattening input | Any `JsonNode` | `JsonObject` |
| Array path syntax | Bracketed: `users:[0]:name` | Bare numeric: `users:0:name` |
| Flattened values | `string?` | Original JSON nodes, or `string?` for the dictionary method |
| Unflattened arrays | Append order; no sparse padding | Numeric indices; sparse indices padded with `null` |
| Best suited to | Configuration and string key/value stores | Lossless JSON round-tripping |

### String-valued operations

`JsonExtensions.Flatten` returns an `IDictionary<string, string?>`, where JSON
null is represented by a null value. `JsonExtensions.Unflatten` creates a new
`JsonObject`, but every non-null leaf becomes a JSON string; numbers and
booleans are not restored to their original JSON kinds.

Array indices in `Unflatten` are applied in the order encountered in the input.
The method appends values rather than padding arrays to the numeric index. Empty
objects and arrays produce no flattened entries and therefore cannot be
represented by a flattened result.

### Type-preserving `JsonObject` operations

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

`ToFlattenedJsonObject` deep-clones leaf nodes, and
`ToUnflattenedJsonObject` deep-clones them again, so these operations do not
share mutable node references with their source. `ToFlattenedDictionary`
stringifies values and cannot preserve their JSON kinds. `HasNestedObjects`
checks only direct property values; it returns `true` for a direct
`JsonObject` or `JsonArray`.

## Replacing values

`ReplaceValue` and `ReplaceValues` modify the supplied `JsonObject` in place and
return the same instance. Replacement values are always stored as JSON strings,
or as JSON null when the supplied value is null.

`ReplaceValue` supports nested object paths but does not interpret array
segments. A simple root-level path is replaced only when that property already
exists. For a separated path, existing object segments are traversed; if a
segment is missing, traversal stops at the deepest object reached and the final
path segment is assigned there. If no object segment exists, that final segment
is assigned on the root object.

`ReplaceValues` additionally supports existing arrays using bare, zero-based
numeric segments such as `users:0:name`. It never creates intermediate objects
or arrays. A missing or out-of-bounds path is left unchanged unless the
document contains the applicable documented literal-path fallback. Empty keys
are ignored, and `[0]` is a literal property name rather than an array index.

See [Replacing values](docs/replacing-values.md) for the complete traversal and
fallback rules.

## Separators and path notation

All path-based methods accept a separator, defaulting to `":"`. Separators can
contain multiple characters:

```csharp
var flat = JsonExtensions.Flatten(json, separator: "__");
var rebuilt = JsonExtensions.Unflatten(flat, separator: "__");
```

Use the same separator for both halves of a round trip, and avoid separators
that occur in property names. Property names are not escaped; a separator in a
property name will be interpreted as a path boundary when unflattening.

The two array conventions are intentionally different:

- `JsonExtensions.Flatten` and `Unflatten` use `[0]`, `[1]`, and so on.
- `JsonObject` extension members and `ReplaceValues` use `0`, `1`, and so on.

Do not pass bracketed paths from `Flatten` directly to `ReplaceValues`, and do
not mix `JsonExtensions.Unflatten` with `ToFlattenedJsonObject`.

## Documentation

| Topic | Description |
| --- | --- |
| [Getting started](docs/getting-started.md) | Installation, namespaces, and guided examples |
| [Path notation](docs/path-notation.md) | Array conventions, separators, values, and edge cases |
| [Flattening and unflattening](docs/flattening.md) | `JsonExtensions.Flatten` and `Unflatten` |
| [JsonObject extension members](docs/jsonobject-extensions.md) | Type-preserving flattening and reconstruction |
| [Replacing values](docs/replacing-values.md) | In-place updates, arrays, and fallbacks |
| [API reference](api/index.md) | Generated DocFX API documentation |

## License

Licensed under the [MIT License](LICENSE.txt).
