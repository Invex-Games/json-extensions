namespace Invex.Extensions.Json;

/// <summary>
///     Extension methods for working with System.Text.Json nodes using a human-readable path notation.
///     <para>Path conventions:</para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Object properties are separated by a configurable separator, colon by default
///                 (e.g., <c>"user:address:city"</c>).
///             </description>
///         </item>
///         <item>
///             <description>
///                 Arrays are addressed with bracketed indices (e.g., <c>"users:[0]:name"</c>) in
///                 flattened/unflattened keys.
///             </description>
///         </item>
///         <item>
///             <description>
///                 For in-place replacement via
///                 <see cref="ReplaceValues(JsonObject, Dictionary{string, string?}, string)" />, array steps use
///                 bare numeric segments instead (e.g., <c>"users:0:name"</c>).
///             </description>
///         </item>
///     </list>
///     <para>
///         These helpers are allocation-conscious and designed for clarity when manipulating JSON in config and ETL
///         scenarios.
///     </para>
/// </summary>
[PublicAPI]
public static partial class JsonExtensions
{
    /// <summary>
    ///     Flattens a hierarchical JSON structure into a flat dictionary of path/value pairs.
    ///     Nested objects are represented using separator-joined paths, and array elements
    ///     use bracket notation with zero-based indices.
    /// </summary>
    /// <param name="node">
    ///     The JSON node to flatten. Can be a <see cref="JsonObject" />, <see cref="JsonArray" />, or primitive value.
    /// </param>
    /// <param name="separator">
    ///     The string used to join path segments. May be more than one character. Defaults to <c>":"</c>.
    /// </param>
    /// <returns>
    ///     A dictionary mapping each leaf path to its string representation:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Keys use <paramref name="separator" /> between object property segments and
    ///                 <c>[index]</c> notation for array elements (e.g., <c>"user:tags:[0]"</c>).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Values are the string representation of the JSON values (<c>null</c> for JSON null values),
    ///                 so the original JSON value kinds are not preserved.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>The source node is not modified.</para>
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Empty nested objects and arrays produce no entries in the output. If <paramref name="node" /> is
    ///         itself a primitive value, the result contains a single entry with an empty-string key.
    ///     </para>
    ///     <para>
    ///         The output can be passed to <see cref="Unflatten(IDictionary{string, string?}, string)" /> to rebuild
    ///         the structure (all values become JSON strings).
    ///     </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="node" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
    /// var json = JsonNode.Parse("""{ "user": { "name": "John", "tags": ["admin", "user"] } }""");
    /// var flattened = JsonExtensions.Flatten(json);
    /// // { ["user:name"] = "John", ["user:tags:[0]"] = "admin", ["user:tags:[1]"] = "user" }
    /// ]]></code>
    /// </example>
    public static IDictionary<string, string?> Flatten(JsonNode node, string separator = ":")
    {
        #if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(node);
        #else
        if (node is null)
            throw new ArgumentNullException(nameof(node));
        #endif

        var keyLookup = new Dictionary<string, string?>();
        var flattened = new Dictionary<string, string?>();
        var sb = new StringBuilder(64);

        Flatten(node, flattened, keyLookup, sb, separator);

        return flattened;
    }

    /// <summary>
    ///     Recursively flattens a JSON node into key/value pairs with path-based keys.
    ///     This is the internal worker that performs the actual flattening logic.
    /// </summary>
    /// <param name="node">The current JSON node to process (can be null).</param>
    /// <param name="flattened">The dictionary that flattened key/value pairs are added to.</param>
    /// <param name="keyLookup">Tracks keys already emitted so duplicates overwrite rather than throw.</param>
    /// <param name="sb">A reusable <see cref="StringBuilder" /> holding the current path being built.</param>
    /// <param name="separator">The string used to join object property segments.</param>
    /// <remarks>
    ///     <para>The method handles three cases:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 <see cref="JsonArray" />: recurses into elements appending <c>[index]</c> notation.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 <see cref="JsonObject" />: recurses into properties appending separator-joined names.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Primitive values (or null): adds the value using the current path as the key, replacing any
    ///                 value previously emitted for the same key.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         The <see cref="StringBuilder" /> is trimmed back to its previous length after each container is
    ///         processed, avoiding per-segment string allocations.
    ///     </para>
    /// </remarks>
    private static void Flatten(
        this JsonNode? node,
        Dictionary<string, string?> flattened,
        Dictionary<string, string?> keyLookup,
        StringBuilder sb,
        string separator)
    {
        switch (node)
        {
            case JsonArray array:
            {
                var baseLen = sb.Length;
                sb.Append(separator);
                sb.Append('[');
                var indexStart = sb.Length;

                for (var i = 0; i < array.Count; i++)
                {
                    sb.Length = indexStart;
                    sb.Append(i);
                    sb.Append(']');
                    Flatten(array[i], flattened, keyLookup, sb, separator);
                }

                sb.Length = baseLen;

                break;
            }

            case JsonObject obj:
            {
                var baseLen = sb.Length;

                if (baseLen > 0)
                    sb.Append(separator);

                var keyStart = sb.Length;

                foreach (var pair in obj)
                {
                    sb.Length = keyStart;
                    sb.Append(pair.Key);
                    Flatten(pair.Value, flattened, keyLookup, sb, separator);
                }

                sb.Length = baseLen;

                break;
            }

            default:
            {
                var key = sb.ToString();
                var value = GetStringValue(node);

                if (keyLookup.TryGetValue(key, out var existingIndex))
                {
                    if (existingIndex is not null)
                        flattened[existingIndex] = value;
                }
                else
                {
                    flattened.Add(key, value);
                    keyLookup.Add(key, flattened.Count.ToString());
                }

                break;
            }
        }
    }

    /// <summary>
    ///     Returns the string value of a leaf <see cref="JsonNode" />, using round-trip formatting for
    ///     floating-point values on platforms where <c>System.Text.Json</c> would otherwise use G17
    ///     (which can produce extra trailing digits, e.g. <c>"3.1400000000000001"</c> instead of
    ///     <c>"3.14"</c>).
    /// </summary>
    /// <param name="node">The leaf node to stringify, or <c>null</c> for a JSON null value.</param>
    /// <returns>The string representation of the value, or <c>null</c> for JSON null.</returns>
    private static string? GetStringValue(JsonNode? node)
    {
        if (node is null)
            return null;

        #if NETSTANDARD2_0
        if (node is JsonValue jsonValue)
        {
            if (jsonValue.TryGetValue<double>(out var d))
                return d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

            if (jsonValue.TryGetValue<float>(out var f))
                return f.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
        #endif

        return node.ToString();
    }
}
