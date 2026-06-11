namespace Invex.Extensions.Json;

public static partial class JsonExtensions
{
    /// <summary>
    ///     Replaces the value at a specified path within a JSON object, modifying the object in place.
    /// </summary>
    /// <param name="root">The JSON object to modify.</param>
    /// <param name="path">
    ///     The property path to replace. Use <paramref name="separator" />-joined segments for nested objects
    ///     (e.g., <c>"user:address:city"</c>). Array indices are not supported by this method.
    ///     If the path contains no <paramref name="separator" />, it is treated as a simple, root-level property name.
    ///     An empty path is ignored and the object is returned unchanged.
    /// </param>
    /// <param name="value">The new value to assign at the specified path. Use <c>null</c> for JSON null.</param>
    /// <param name="separator">The string used to split path segments. Defaults to <c>":"</c>.</param>
    /// <returns>
    ///     The same <see cref="JsonObject" /> instance passed in, after modification:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 For simple paths (no <paramref name="separator" />), the property is only replaced if it
    ///                 already exists; missing properties are not added.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 For <paramref name="separator" />-joined paths, only existing intermediate
    ///                 <see cref="JsonObject" /> nodes are traversed; missing segments are not created. The final
    ///                 segment is then set on the deepest object reached (potentially the root), adding the property
    ///                 there if it does not exist.
    ///             </description>
    ///         </item>
    ///     </list>
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is intentionally conservative about creating structure. If you need batch updates and
    ///         support for stepping into arrays, see
    ///         <see cref="ReplaceValues(JsonObject, Dictionary{string, string?}, string)" />.
    ///     </para>
    ///     <para>
    ///         All replacement values are stored as JSON strings (or JSON null); existing value kinds such as
    ///         numbers or booleans are not preserved at the target path.
    ///     </para>
    ///     <para>
    ///         If <paramref name="root" /> is null, a <see cref="NullReferenceException" /> is thrown when the
    ///         object is first dereferenced, per extension method invocation semantics.
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
    /// var obj = JsonNode.Parse("""{ "user": { "name": "John" } }""").AsObject();
    /// obj.ReplaceValue("user:name", "Jane");
    /// // {"user":{"name":"Jane"}}
    /// obj.ReplaceValue("unknown", "x");
    /// // Unchanged: simple paths never add missing root-level properties.
    /// ]]></code>
    /// </example>
    public static JsonObject ReplaceValue(this JsonObject root, string path, string? value, string separator = ":")
    {
        #if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(path);
        #else
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        #endif

        // Ignore empty paths for consistency with batch Replace
        if (path.Length is 0)
            return root;

        // If the path doesn't contain separator, handle as a simple key replacement
        if (!path.Contains(separator))
        {
            if (root.ContainsKey(path))
                root[path] = JsonValue.Create(value);

            return root;
        }

        // Handle nested path replacement
        var pathSegments = path.Split(separator);
        var current = root;

        // Navigate to the parent of the target key
        for (var i = 0; i < pathSegments.Length - 1; i++)
        {
            var segment = pathSegments[i];

            if (current[segment] is JsonObject nestedObj)
                current = nestedObj;
        }

        // Set the value at the final path segment
        var finalKey = pathSegments[^1];
        current[finalKey] = JsonValue.Create(value);

        return root;
    }
}
