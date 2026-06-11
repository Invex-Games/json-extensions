namespace Invex.Extensions.Json;

public static partial class JsonExtensions
{
    /// <summary>
    ///     Applies multiple replacements to a JSON object in place using a consistent path notation.
    /// </summary>
    /// <param name="root">The root JSON object to modify.</param>
    /// <param name="replacements">
    ///     A mapping from path to new value. Paths can be:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Simple property names (e.g., <c>"name"</c>); only updated if the property exists on the root.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <paramref name="separator" />-joined nested paths (e.g., <c>"user:address:city"</c>).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Paths that step into arrays using bare numeric segments only (e.g., <c>"users:0:name"</c>).
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Note: bracketed indices like <c>[0]</c> are not interpreted as array indices by this method; they
    ///         are treated as ordinary property names. Entries with empty keys are skipped.
    ///     </para>
    /// </param>
    /// <param name="separator">The string used to split path segments. Defaults to <c>":"</c>.</param>
    /// <returns>The same <see cref="JsonObject" /> instance passed in, after modifications.</returns>
    /// <remarks>
    ///     <para>Behavior for each entry:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Simple keys (no <paramref name="separator" />): the root property is updated only if it
    ///                 already exists.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Nested paths: the structure is traversed segment by segment. The final value is set only if
    ///                 the full path exists (the last object property must already exist; array indices must be in
    ///                 bounds).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Fallbacks when traversal cannot complete: if an intermediate object is missing a segment, the
    ///                 remaining <paramref name="separator" />-joined path is set as a literal property on the
    ///                 deepest object reached (added if absent); otherwise, if the root itself contains a literal
    ///                 property whose name equals the full key, that property is updated.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>No intermediate containers (objects or arrays) are ever created.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Values are stored using <see cref="JsonValue.Create(string?, JsonNodeOptions?)" />, so all
    ///                 replacement values become JSON strings (or JSON null); existing value kinds are not preserved
    ///                 at the target path.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>This method modifies <paramref name="root" /> in place and returns it for chaining.</para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="replacements" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
    /// var obj = JsonNode.Parse("""{ "name": "John", "user": { "address": { "city":"NYC" } }, "users": [ { "name":"A" }, { "name":"B" } ] }""").AsObject();
    /// obj.ReplaceValues(new Dictionary<string,string?>
    /// {
    ///     ["name"] = "Jane",             // replaces root-level property if present
    ///     ["user:address:city"] = "LA",  // traverses nested objects if present
    ///     ["users:1:name"] = "Beta",     // updates within existing arrays (bare numeric index)
    /// });
    /// // {"name":"Jane","user":{"address":{"city":"LA"}},"users":[{"name":"A"},{"name":"Beta"}]}
    /// ]]></code>
    /// </example>
    public static JsonObject ReplaceValues(
        this JsonObject root,
        Dictionary<string, string?> replacements,
        string separator = ":")
    {
        #if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(replacements);
        #else
        if (replacements is null)
            throw new ArgumentNullException(nameof(replacements));
        #endif

        foreach (var (key, newValue) in replacements)
        {
            if (key.Length is 0)
                continue;

            // If key contains separator, attempt nested update first (preserve nested structure if it exists),
            // otherwise fall back to literal property update on the root if it exists.
            if (key.Contains(separator))
            {
                if (TrySetNestedIfExists(root, key, newValue, separator))
                    continue;

                if (root.TryGetPropertyValue(key, out _))
                    root[key] = JsonValue.Create(newValue);

                continue;
            }

            // Simple root-level property update if it exists.
            if (root.TryGetPropertyValue(key, out _))
                root[key] = JsonValue.Create(newValue);
        }

        return root;
    }

    /// <summary>
    ///     Attempts to set a value at a separated path, stepping through objects and arrays (bare numeric segments
    ///     index into arrays). Never creates intermediate containers.
    /// </summary>
    /// <param name="root">The root object to start from.</param>
    /// <param name="path">A <paramref name="separator" />-joined path; array positions use bare numeric segments.</param>
    /// <param name="value">The value to set (<c>null</c> for JSON null).</param>
    /// <param name="separator">The string used to split path segments.</param>
    /// <returns><c>true</c> if a value was set; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <para>
    ///         If an intermediate object lacks the next segment, the remaining joined path is written as a literal
    ///         property on that object (added if absent) and the method returns <c>true</c>.
    ///     </para>
    ///     <para>
    ///         The final segment is set only if it already exists (object property present, or array index in
    ///         bounds); traversal through a primitive value or an out-of-bounds/non-numeric array segment returns
    ///         <c>false</c>.
    ///     </para>
    /// </remarks>
    private static bool TrySetNestedIfExists(JsonObject root, string path, string? value, string separator)
    {
        var parts = path.Split(separator);

        if (parts.Length == 0)
            return false;

        JsonNode? current = root;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            var segment = parts[i];

            switch (current)
            {
                case JsonObject obj:
                    if (obj.TryGetPropertyValue(segment, out var next))
                    {
                        current = next;

                        continue;
                    }

                    // Fallback: if the nested object path doesn't exist, check if the current object
                    // contains a literal property with the remaining joined path and update it.
                    #if NET8_0_OR_GREATER
                    var remainingPath = string.Join(separator, parts[i..]);
                    #else
                    var remainingPath = string.Join(separator, parts.Skip(i));
                    #endif

                    obj[remainingPath] = JsonValue.Create(value);

                    return true;

                case JsonArray arr:
                    if (!int.TryParse(segment, out var index) || index < 0 || index >= arr.Count)
                        return false;

                    current = arr[index];

                    continue;

                default:
                    return false;
            }
        }

        var last = parts[^1];

        switch (current)
        {
            case JsonObject obj:
                if (!obj.TryGetPropertyValue(last, out _))
                    return false;

                obj[last] = JsonValue.Create(value);

                return true;

            case JsonArray arr:
                if (!int.TryParse(last, out var index) || index < 0 || index >= arr.Count)
                    return false;

                arr[index] = JsonValue.Create(value);

                return true;

            default:
                return false;
        }
    }
}
