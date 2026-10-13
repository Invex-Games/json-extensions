# Getting Started

This page takes you from installing the package to flattening, rebuilding, and updating your first
`System.Text.Json` node tree. Read [Introduction](introduction.md) first if you are still deciding
which API surface fits your problem.

## Installation

```shell
dotnet add package Invex.Extensions.Json
```

Supported target frameworks: `net10.0`, `net9.0`, `net8.0`, and `netstandard2.0`
(the .NET Standard build references `System.Text.Json` as a package).

## Namespaces

Everything lives in a single namespace:

```csharp
using System.Text.Json.Nodes;   // JsonNode, JsonObject, JsonArray
using Invex.Extensions.Json;    // JsonExtensions, JsonUtil
```

## The two API surfaces

The library exposes two complementary APIs. They solve the same problems with slightly
different trade-offs — see [Introduction](introduction.md#two-api-surfaces) for when to use each
and [Path Notation](path-notation.md) for the full comparison.

### 1. `JsonExtensions` — string-valued, bracketed array indices

Best when the destination is a flat key/value store (configuration providers, environment
variables, .properties-style files) where everything is a string anyway.

```csharp
var json = JsonNode.Parse("""
{
  "user": {
    "name": "John",
    "addresses": [ { "city": "New York", "zip": "10001" } ],
    "tags": ["admin", "user"]
  }
}
""")!;

// Flatten — array elements use [index] notation
IDictionary<string, string?> flat = JsonExtensions.Flatten(json);
// ["user:name"]                = "John"
// ["user:addresses:[0]:city"]  = "New York"
// ["user:addresses:[0]:zip"]   = "10001"
// ["user:tags:[0]"]            = "admin"
// ["user:tags:[1]"]            = "user"

// Unflatten — rebuilds the hierarchy (all leaf values become JSON strings)
JsonObject rebuilt = JsonExtensions.Unflatten(flat);
```

### 2. `JsonObject` extension members — type-preserving, bare numeric indices

Best when you need lossless round-tripping: numbers stay numbers, booleans stay booleans.

```csharp
var obj = JsonNode.Parse("""
{
  "user": { "age": 42, "active": true, "tags": ["admin", "user"] }
}
""")!.AsObject();

// Flatten into another JsonObject — values keep their JSON types
JsonObject flatObj = obj.ToFlattenedJsonObject();
// {"user:age":42,"user:active":true,"user:tags:0":"admin","user:tags:1":"user"}

// Or flatten into a Dictionary<string, string?>
Dictionary<string, string?> flatDict = obj.ToFlattenedDictionary();
// ["user:age"] = "42", ["user:active"] = "true", ...

// Rebuild the hierarchy (numeric segments become array indices)
JsonObject roundTrip = flatObj.ToUnflattenedJsonObject();

// Quick check: is this object already flat?
bool nested = obj.HasNestedObjects(); // true
```

## Updating values

Both single and batch replacement modify the `JsonObject` **in place** and are deliberately
conservative — they never create intermediate containers:

```csharp
var obj = JsonNode.Parse("""
{ "name": "John", "user": { "address": { "city": "NYC" } }, "users": [ { "name": "A" } ] }
""")!.AsObject();

// Single value
obj.ReplaceValue("user:address:city", "LA");

// Batch — supports stepping into arrays with bare numeric segments
obj.ReplaceValues(new Dictionary<string, string?>
{
    ["name"]         = "Jane",
    ["users:0:name"] = "Alpha",
});
```

See [Replacing Values](replacing-values.md) for the full semantics, including fallback
behavior when a path doesn't exist.

## Custom separators

Every method accepts a `separator` parameter (default `":"`). Separators may be longer than
one character:

```csharp
var flat = JsonExtensions.Flatten(json, separator: "__");
// ["user__name"] = "John", ...

var rebuilt = JsonExtensions.Unflatten(flat, separator: "__");
```

> [!IMPORTANT]
> Choose a separator that never occurs in your property names — otherwise unflattening will
> split those names incorrectly. Always use the **same separator** for flattening and
> unflattening.

## Next steps

- [Path Notation](path-notation.md) — how paths are formed and parsed
- [Flattening and Unflattening](flattening.md) — `JsonExtensions` in depth
- [JsonObject Extension Members](jsonobject-extensions.md) — the type-preserving API
- [Replacing Values](replacing-values.md) — update semantics and fallbacks
- <xref:Invex.Extensions.Json.JsonExtensions> and <xref:Invex.Extensions.Json.JsonUtil> in the API reference

