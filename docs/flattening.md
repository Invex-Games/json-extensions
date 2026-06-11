# Flattening & unflattening with `JsonExtensions`

`JsonExtensions.Flatten` and `JsonExtensions.Unflatten` convert between hierarchical JSON and a
flat `IDictionary<string, string?>` using **bracketed array indices** (`users:[0]:name`). All
values are represented as strings, which makes these methods a natural fit for configuration
providers, environment variables, and other key/value stores.

## Flatten

```csharp
public static IDictionary<string, string?> Flatten(JsonNode node, string separator = ":")
```

Walks the tree depth-first and emits one entry per **leaf value**:

```csharp
var json = JsonNode.Parse("""
{
  "user": {
    "name": "John",
    "addresses": [ { "city": "New York", "zip": "10001" } ],
    "tags": ["admin", "user"],
    "manager": null
  }
}
""")!;

var flat = JsonExtensions.Flatten(json);
// ["user:name"]               = "John"
// ["user:addresses:[0]:city"] = "New York"
// ["user:addresses:[0]:zip"]  = "10001"
// ["user:tags:[0]"]           = "admin"
// ["user:tags:[1]"]           = "user"
// ["user:manager"]            = null
```

### Behavior notes

- **Input** — accepts any `JsonNode` (`JsonObject`, `JsonArray`, or a primitive). The source
  node is never modified.
- **Values become strings** — numbers and booleans are stored via `ToString()`; JSON nulls
  become `null` dictionary entries. The original value kinds are *not* preserved.
- **Empty containers** — `{}` and `[]` produce no entries.
- **Primitive root** — produces a single entry with an empty-string key.
- **Performance** — implemented with a single reusable `StringBuilder`; no regex, no
  per-segment string allocations.
- **Throws** — `ArgumentNullException` when `node` is null.

## Unflatten

```csharp
public static JsonObject Unflatten(IDictionary<string, string?> flattened, string separator = ":")
```

Reverses `Flatten`, recreating nested objects and arrays from the path notation:

```csharp
var flattened = new Dictionary<string, string?>
{
    ["user:name"]               = "John",
    ["user:addresses:[0]:city"] = "New York",
    ["user:addresses:[0]:zip"]  = "10001",
    ["user:tags:[0]"]           = "admin",
    ["user:tags:[1]"]           = "user",
};

JsonObject json = JsonExtensions.Unflatten(flattened);
// {"user":{"name":"John","addresses":[{"city":"New York","zip":"10001"}],"tags":["admin","user"]}}
```

### Behavior notes

- **Bracketed segments** (`[0]`, `[1]`, …) indicate array positions; everything else is an
  object property name. Mixed paths like `users:[0]:name` are fully supported.
- **Append-order indices** — array elements are added in the order encountered; sparse or
  non-sequential indices are *not* padded with nulls. (Contrast with
  [`ToUnflattenedJsonObject`](jsonobject-extensions.md#tounflattenedjsonobject), which pads.)
- **All leaves are strings** — because the input values are strings, the rebuilt tree contains
  JSON strings (or JSON nulls) at every leaf. Numbers and booleans are not restored.
- **Duplicate paths** — the last value wins.
- **Throws** — `ArgumentNullException` when `flattened` is null.

## Round-tripping

`Flatten` → `Unflatten` preserves *structure* but normalizes all leaf values to strings:

```csharp
var original = JsonNode.Parse("""{ "n": 42, "b": true }""")!;
var roundTripped = JsonExtensions.Unflatten(JsonExtensions.Flatten(original));
// {"n":"42","b":"true"}  ← note: strings now
```

If you need lossless round-tripping, use the
[`JsonObject` extension members](jsonobject-extensions.md) instead.

## Custom separators

```csharp
var flat = JsonExtensions.Flatten(json, separator: "__");
// ["user__tags__[0]"] = "admin"

var rebuilt = JsonExtensions.Unflatten(flat, separator: "__");
```

The separator may be multi-character. Use the same separator in both directions, and pick one
that never occurs in property names.

