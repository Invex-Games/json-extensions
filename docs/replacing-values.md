# Replacing values

`ReplaceValue` (single path) and `ReplaceValues` (batch) update values inside an existing
`JsonObject` **in place**, returning the same instance for chaining. Both are deliberately
conservative: they **never create intermediate objects or arrays**, so a typo'd path can't
silently grow your document structure.

```csharp
using System.Text.Json.Nodes;
using Invex.Extensions.Json;
```

> [!NOTE]
> All replacement values are stored as JSON strings (or JSON null). If the target previously
> held a number or boolean, it becomes a string after replacement.

## ReplaceValue — single path

```csharp
public static JsonObject ReplaceValue(this JsonObject root, string path, string? value, string separator = ":")
```

```csharp
var obj = JsonNode.Parse("""{ "user": { "name": "John" } }""")!.AsObject();

obj.ReplaceValue("user:name", "Jane");
// {"user":{"name":"Jane"}}

obj.ReplaceValue("unknown", "x");
// Unchanged — simple paths never add missing root-level properties.
```

Semantics:

| Path shape | Behavior |
| --- | --- |
| Empty (`""`) | Ignored; object returned unchanged. |
| Simple (no separator) | Replaced **only if the property already exists** on the root. Missing properties are not added. |
| Nested (`a:b:c`) | Existing intermediate `JsonObject` nodes are traversed; missing segments are *not* created. The final segment is set on the deepest object reached (potentially the root), adding the property there if absent. |

Array indices are **not** supported by `ReplaceValue` — use `ReplaceValues` for that.
Setting `value` to `null` writes a JSON null. Throws `ArgumentNullException` when `path` is
null.

## ReplaceValues — batch

```csharp
public static JsonObject ReplaceValues(this JsonObject root, Dictionary<string, string?> replacements, string separator = ":")
```

```csharp
var obj = JsonNode.Parse("""
{
  "name": "John",
  "user": { "address": { "city": "NYC" } },
  "users": [ { "name": "A" }, { "name": "B" } ]
}
""")!.AsObject();

obj.ReplaceValues(new Dictionary<string, string?>
{
    ["name"]              = "Jane",   // root property — updated if present
    ["user:address:city"] = "LA",     // nested objects — traversed if present
    ["users:1:name"]      = "Beta",   // arrays — bare numeric index, must be in bounds
});
// {"name":"Jane","user":{"address":{"city":"LA"}},"users":[{"name":"A"},{"name":"Beta"}]}
```

Per-entry semantics:

1. **Empty keys** are skipped.
2. **Simple keys** (no separator): the root property is updated only if it already exists.
3. **Nested paths**: traversed segment by segment through objects and arrays. Bare numeric
   segments index into arrays and must be **in bounds**; the final object property must
   already exist.
4. **Fallbacks** when traversal cannot complete:
   - If an intermediate *object* is missing a segment, the remaining separator-joined path is
     written as a **literal property** on the deepest object reached (added if absent). This
     supports documents that themselves use flattened keys, e.g. a root property literally
     named `"a:b:c"`.
   - Otherwise, if the root contains a literal property whose name equals the full key, that
     property is updated.
5. **No containers are ever created** — failed array traversals (out-of-bounds, non-numeric
   segment, or traversal through a primitive) leave the document untouched for that entry.

> [!WARNING]
> Bracketed indices such as `[0]` are **not** interpreted as array indices by `ReplaceValues` —
> they are treated as ordinary property names. Always use bare numeric segments
> (`users:0:name`) with this method.

Throws `ArgumentNullException` when `replacements` is null.

## Worked examples

### Updating array elements

```csharp
var obj = JsonNode.Parse("""{ "tags": ["a", "b", "c"] }""")!.AsObject();

obj.ReplaceValues(new() { ["tags:1"] = "B" });
// {"tags":["a","B","c"]}

obj.ReplaceValues(new() { ["tags:9"] = "X" });
// Unchanged — index out of bounds, nothing created.
```

### Literal flattened-key fallback

```csharp
var obj = JsonNode.Parse("""{ "feature:flags:dark-mode": "off" }""")!.AsObject();

obj.ReplaceValues(new() { ["feature:flags:dark-mode"] = "on" });
// {"feature:flags:dark-mode":"on"} — matched as a literal root property.
```

### Chaining

```csharp
var result = obj
    .ReplaceValue("user:name", "Jane")
    .ReplaceValues(new() { ["users:0:name"] = "Alpha" });
```

## ReplaceValue vs. ReplaceValues

| | `ReplaceValue` | `ReplaceValues` |
| --- | --- | --- |
| Paths per call | One | Many |
| Array support | No | Yes (bare numeric, in-bounds only) |
| Missing final segment (nested path) | Added on deepest object reached | Literal remaining-path fallback on deepest object reached |
| Missing root property (simple path) | Not added | Not added |
| Mutates in place | Yes | Yes |

