# `JsonObject` extension members (`JsonUtil`)

The `JsonUtil` class provides **extension members directly on `JsonObject`** for flattening and
unflattening with **bare numeric array indices** (`users:0:name`) and — uniquely —
**full preservation of JSON value kinds** when using `ToFlattenedJsonObject`.

```csharp
using System.Text.Json.Nodes;
using Invex.Extensions.Json;
```

## ToFlattenedJsonObject

```csharp
public JsonObject ToFlattenedJsonObject(string separator = ":")
```

Flattens into a *new* single-level `JsonObject`. Each value is a **deep clone** of the original
leaf node, so strings, numbers, booleans, and nulls all keep their JSON types:

```csharp
var obj = JsonNode.Parse("""
{ "user": { "age": 42, "active": true, "tags": ["admin", "user"] } }
""")!.AsObject();

JsonObject flat = obj.ToFlattenedJsonObject();
// {"user:age":42,"user:active":true,"user:tags:0":"admin","user:tags:1":"user"}
```

- Keys appear in depth-first, insertion order.
- The source object is not modified, and no node references are shared with it.
- JSON nulls are preserved as null values.
- Empty objects/arrays produce no entries (and are therefore lost on round-trip).

## ToFlattenedDictionary

```csharp
public Dictionary<string, string?> ToFlattenedDictionary(string separator = ":")
```

Same traversal, but values are converted to strings — convenient for feeding key/value sinks:

```csharp
Dictionary<string, string?> flat = obj.ToFlattenedDictionary();
// ["user:age"]    = "42"     (raw JSON text)
// ["user:active"] = "true"   (booleans → "true"/"false")
// ["user:tags:0"] = "admin"  (strings verbatim)
```

JSON nulls become `null` entries. Because everything is stringified, value kinds are not
recoverable — prefer `ToFlattenedJsonObject` when type fidelity matters.

## ToUnflattenedJsonObject

```csharp
public JsonObject ToUnflattenedJsonObject(string separator = ":")
```

Rebuilds the hierarchy from a flattened `JsonObject`, reversing `ToFlattenedJsonObject`:

```csharp
var flat = new JsonObject
{
    ["user:name"]   = "John",
    ["user:tags:0"] = "admin",
    ["user:tags:1"] = "user",
};

JsonObject rebuilt = flat.ToUnflattenedJsonObject();
// {"user":{"name":"John","tags":["admin","user"]}}
```

Container inference and array handling:

- A **purely numeric segment** causes its parent container to be created as a `JsonArray`; any
  other segment creates a `JsonObject`. Property names that are purely numeric are therefore
  reconstructed as array elements.
- **Sparse indices are padded** with nulls: a lone key `items:2` yields `[null, null, value]`.
  (Contrast with [`JsonExtensions.Unflatten`](flattening.md#unflatten), which appends instead.)
- Leaf values are **deep clones**; the source object is not modified.
- Duplicate paths: the last value wins.
- Throws `InvalidOperationException` if one key requires an array where another key already
  forced a non-numeric segment (structural conflict).

## HasNestedObjects

```csharp
public bool HasNestedObjects()
```

A quick top-level check for whether an object is already flat:

```csharp
var a = JsonNode.Parse("""{ "x": 1, "y": "z" }""")!.AsObject();
a.HasNestedObjects(); // false

var b = JsonNode.Parse("""{ "x": { "y": 1 } }""")!.AsObject();
b.HasNestedObjects(); // true (direct property value is an object or array)
```

Only direct property values are inspected — it does not recurse.

## Lossless round-trip example

```csharp
var original = JsonNode.Parse("""
{ "n": 42, "b": true, "arr": [1, null, "three"], "nested": { "deep": 3.14 } }
""")!.AsObject();

var roundTripped = original
    .ToFlattenedJsonObject()
    .ToUnflattenedJsonObject();

// JsonNode.DeepEquals(original, roundTripped) == true
// (numbers are still numbers, booleans still booleans)
```

> [!NOTE]
> This API uses **bare numeric** array segments and is not interchangeable with the bracketed
> notation used by `JsonExtensions.Flatten`/`Unflatten`. See
> [Path notation](path-notation.md) for the full comparison.

