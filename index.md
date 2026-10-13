# Invex.Extensions.Json

`Invex.Extensions.Json` is a small, allocation-conscious library for flattening, unflattening,
and updating `System.Text.Json` node trees (`JsonNode`, `JsonObject`, and `JsonArray`) with
readable paths such as `user:address:city`. It is useful for configuration and
environment-variable adapters, ETL pipelines, JSON diffing or patching, and exchanging JSON with
flat key/value stores.

## Start here

- [Introduction](docs/introduction.md): what the library is for and which API surface to pick.
- [Getting Started](docs/getting-started.md): install the package and run your first examples.
- [Path Notation](docs/path-notation.md): how paths, separators, and array indices work.
- [Replacing Values](docs/replacing-values.md): update an existing `JsonObject` in place.
- [API Reference](api/index.md): every public type and member.
