# API Reference

Generated reference for **Invex.Extensions.Json**, produced from the XML documentation comments in
the source code.

## Invex.Extensions.Json

| Type | Kind | Description |
| --- | --- | --- |
| [`JsonExtensions`](xref:Invex.Extensions.Json.JsonExtensions) | Static class | Flattens any `JsonNode` to string key/value pairs (`Flatten`), rebuilds the hierarchy (`Unflatten`), and updates values in place (`ReplaceValue`, `ReplaceValues`). Uses bracketed array indices (`users:[0]:name`) for flatten/unflatten and bare numeric segments (`users:0:name`) for replacement. |
| [`JsonUtil`](xref:Invex.Extensions.Json.JsonUtil) | Static class (extension members) | Extension members on `JsonObject` for type-preserving flattening (`ToFlattenedJsonObject`), string flattening (`ToFlattenedDictionary`), reconstruction with sparse-array padding (`ToUnflattenedJsonObject`), and a quick nesting check (`HasNestedObjects`). Uses bare numeric array segments throughout. |

## Typical flow

```csharp
using System.Text.Json.Nodes;
using Invex.Extensions.Json;

var source = JsonNode.Parse("""{ "user": { "age": 42, "tags": ["admin"] } }""")!.AsObject();

// Lossless round trip with bare numeric indices.
JsonObject flat = source.ToFlattenedJsonObject();      // {"user:age":42,"user:tags:0":"admin"}
JsonObject rebuilt = flat.ToUnflattenedJsonObject();

// Update the rebuilt document in place.
rebuilt.ReplaceValues(new Dictionary<string, string?> { ["user:tags:0"] = "editor" });
```

For guided usage, start with the [Introduction](../docs/introduction.md).
