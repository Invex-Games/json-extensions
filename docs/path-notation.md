# Path notation

All APIs in this library address locations inside a JSON tree with a **path string**: a list of
segments joined by a configurable separator (default `":"`). Object properties are always
addressed by name; the difference between the two API surfaces is **how array elements are
addressed**.

## The two conventions at a glance

Given this document:

```json
{
  "user": {
    "name": "John",
    "tags": ["admin", "user"]
  }
}
```

| Leaf | `JsonExtensions` (bracketed) | `JsonObject` members (bare numeric) |
| --- | --- | --- |
| `"John"` | `user:name` | `user:name` |
| `"admin"` | `user:tags:[0]` | `user:tags:0` |
| `"user"` | `user:tags:[1]` | `user:tags:1` |

### Bracketed indices — `JsonExtensions.Flatten` / `Unflatten`

- Array elements are written as `[index]`, e.g., `users:[0]:name`.
- Because array segments are syntactically distinct from property names, a property literally
  named `"0"` is never confused with an array index.
- During `Unflatten`, indices are applied in **append order**: sparse or out-of-order indices
  are not padded with nulls.

### Bare numeric indices — `JsonUtil` extension members & `ReplaceValues`

- Array elements are written as plain numbers, e.g., `users:0:name`.
- During `ToUnflattenedJsonObject`, a **purely numeric segment** causes its parent container to
  be created as a `JsonArray`; anything else creates a `JsonObject`. This means property names
  that are purely numeric are indistinguishable from array indices and will be reconstructed as
  array elements.
- Sparse indices **are padded**: a single key `items:2` produces `[null, null, value]`.
- `ReplaceValues` uses this same convention for stepping into arrays, but never creates
  containers — indices must already exist and be in bounds.

> [!WARNING]
> The two conventions are not interchangeable. `ReplaceValues` treats a bracketed segment such
> as `[0]` as an ordinary property name, and `JsonExtensions.Unflatten` treats a bare `0` as a
> property name rather than an index. Always pair `Flatten` with `Unflatten`, and
> `ToFlattenedJsonObject` with `ToUnflattenedJsonObject`.

## Separators

- The default separator is `":"`.
- Separators may be **multi-character** (e.g., `"__"` for environment-variable-style keys).
- The same separator must be used to flatten and unflatten a given document.
- Pick a separator that cannot occur in property names. If a property name contains the
  separator, flattening succeeds, but unflattening will split that name into multiple segments.

```csharp
// Environment-variable style
var flat = JsonExtensions.Flatten(json, separator: "__");
// ["user__tags__[0]"] = "admin"
```

## Value representation

| | `JsonExtensions` | `ToFlattenedJsonObject` | `ToFlattenedDictionary` |
| --- | --- | --- | --- |
| Strings | verbatim | original `JsonValue` (deep clone) | verbatim |
| Numbers | `ToString()` text | original `JsonValue` (deep clone) | raw JSON text |
| Booleans | `ToString()` text | original `JsonValue` (deep clone) | `"true"` / `"false"` |
| JSON null | `null` | `null` | `null` |

Only `ToFlattenedJsonObject` → `ToUnflattenedJsonObject` round-trips **losslessly** with
respect to JSON value kinds. The string-based APIs rebuild every leaf as a JSON string.

## Edge cases

- **Empty containers** — empty objects (`{}`) and empty arrays (`[]`) produce no flattened
  entries, so they are lost on round-trip with every API.
- **Primitive root** — `JsonExtensions.Flatten` accepts any `JsonNode`; flattening a bare
  primitive produces a single entry with an empty-string key.
- **Duplicate paths** — when the same path occurs more than once in the input to an unflatten
  operation, the last value wins.

