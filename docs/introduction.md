# Introduction

`Invex.Extensions.Json` adds path-based operations to the `System.Text.Json` DOM
(`JsonNode`, `JsonObject`, `JsonArray`): flatten a tree into `path → value` pairs, rebuild a tree
from those pairs, and update values in an existing object by path. Read this page to decide whether
the library fits your problem and which of its two API surfaces to use.

## When to use it

- **Flat key/value stores.** Configuration providers, environment variables, and
  `.properties`-style files that only understand `key = value` strings.
- **ETL and data exchange.** Moving JSON documents through systems that store columns or
  key/value pairs rather than trees.
- **Diffing and patching.** Comparing two documents path by path, or applying a set of
  `path → value` overrides to a template document.
- **Inspecting structure.** Checking whether an object is already flat before deciding how to
  process it.

## When not to use it

- **Object serialization.** To map JSON to and from .NET types, use `JsonSerializer`; this
  library works on the node DOM only.
- **Query languages.** It does not implement JSONPath, JSON Pointer, or JSON Patch. Paths are
  plain separator-joined segments with no wildcards, filters, or escaping.
- **Property names that contain the separator.** Names are not escaped, so a separator inside a
  name becomes a path boundary when unflattening. Choose a different separator, or a different
  tool.
- **Empty containers that must survive.** Empty objects and arrays produce no flattened entries,
  so they are lost on every round trip.

## Key concepts at a glance

| Concept | Meaning |
| --- | --- |
| Path | Segments joined by a separator, e.g. `user:address:city`. |
| Separator | The string between segments. Defaults to `":"`; may be several characters, e.g. `"__"`. |
| Leaf | A JSON primitive or JSON null. Only leaves produce flattened entries. |
| Bracketed index | `users:[0]:name`, used by `JsonExtensions.Flatten` and `Unflatten`. |
| Bare numeric index | `users:0:name`, used by the `JsonObject` extension members and `ReplaceValues`. |

### Two API surfaces

The library has two complementary path and value models. Pair operations from the same column
when round-tripping.

| | `JsonExtensions` | `JsonObject` extension members (`JsonUtil`) |
| --- | --- | --- |
| Flattening input | Any `JsonNode` | `JsonObject` |
| Array path syntax | Bracketed: `users:[0]:name` | Bare numeric: `users:0:name` |
| Flattened values | `string?` | Original JSON nodes, or `string?` for the dictionary method |
| Unflattened arrays | Append order; no sparse padding | Numeric indices; sparse indices padded with `null` |
| Best suited to | Configuration and string key/value stores | Lossless JSON round-tripping |

Pick `JsonExtensions` when the destination only holds strings anyway. Pick the `JsonObject`
extension members (`ToFlattenedJsonObject` and `ToUnflattenedJsonObject`) when numbers must stay
numbers and booleans must stay booleans. In both cases, use `ReplaceValue` or `ReplaceValues` to
change values in a document you already have.

> [!WARNING]
> The two array conventions are not interchangeable. Do not pass bracketed paths from `Flatten`
> to `ReplaceValues`, and do not mix `JsonExtensions.Unflatten` with `ToFlattenedJsonObject`.

## Design principles

- **Readable paths over a query language.** One path format, one separator, no escaping rules to
  learn.
- **Sources are not modified.** Flattening and unflattening always return new objects; the
  `JsonObject` members deep-clone leaf nodes so results share no mutable nodes with their input.
- **Replacement is conservative.** `ReplaceValue` and `ReplaceValues` mutate in place but never
  create intermediate objects or arrays, so a mistyped path cannot silently grow the document.
- **Allocation-conscious.** `JsonExtensions.Flatten` builds every path in one reusable
  `StringBuilder`, and no operation uses reflection or regular expressions.

## See also

- [Getting Started](getting-started.md)
- [Path Notation](path-notation.md)
- [Flattening and Unflattening](flattening.md)
- [JsonObject Extension Members](jsonobject-extensions.md)
- [Replacing Values](replacing-values.md)
- <xref:Invex.Extensions.Json.JsonExtensions>
- <xref:Invex.Extensions.Json.JsonUtil>
