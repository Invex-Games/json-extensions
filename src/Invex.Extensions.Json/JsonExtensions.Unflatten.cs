namespace Invex.Extensions.Json;

public static partial class JsonExtensions
{
    /// <summary>
    ///     Reconstructs a hierarchical JSON object from flattened key/value pairs, reversing
    ///     <see cref="Flatten(JsonNode, string)" />.
    ///     Keys are parsed as separator-joined paths in which bracketed segments (<c>[0]</c>, <c>[1]</c>, ...)
    ///     denote array indices.
    /// </summary>
    /// <param name="flattened">
    ///     A dictionary of flattened key/value pairs where:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Key: a path using <paramref name="separator" /> between object property segments and
    ///                 <c>[index]</c> notation for array elements (e.g., <c>"user:tags:[0]"</c>).
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Value: the string representation of the value (<c>null</c> for JSON null values).
    ///             </description>
    ///         </item>
    ///     </list>
    /// </param>
    /// <param name="separator">
    ///     The string used to split keys into path segments. Must match the separator used when flattening.
    ///     Defaults to <c>":"</c>.
    /// </param>
    /// <returns>
    ///     A new <see cref="JsonObject" /> representing the reconstructed hierarchical structure.
    ///     Nested objects and arrays are created as needed based on the path notation; all leaf values are stored
    ///     as JSON strings (or JSON null).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="flattened" /> is null.</exception>
    /// <example>
    ///     <code><![CDATA[
    /// var flattened = new Dictionary<string, string?>
    /// {
    ///     ["user:name"] = "John",
    ///     ["user:addresses:[0]:city"] = "New York",
    ///     ["user:addresses:[0]:zip"] = "10001",
    ///     ["user:tags:[0]"] = "admin",
    ///     ["user:tags:[1]"] = "user",
    /// };
    /// var json = JsonExtensions.Unflatten(flattened);
    /// // {"user":{"name":"John","addresses":[{"city":"New York","zip":"10001"}],"tags":["admin","user"]}}
    /// ]]></code>
    /// </example>
    /// <remarks>
    ///     <para>The method handles common scenarios including:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Creating nested objects and arrays as needed.</description>
    ///         </item>
    ///         <item>
    ///             <description>Managing transitions between object and array contexts.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Overwriting existing values when the same path is encountered multiple times.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Note: array indices are applied in append order; sparse or non-sequential indices are not padded
    ///         with nulls.
    ///     </para>
    ///     <para>Path parsing rules:</para>
    ///     <list type="bullet">
    ///         <item>
    ///             <description><paramref name="separator" /> separates object property names.</description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Square brackets with numbers (<c>[0]</c>, <c>[1]</c>, etc.) indicate array indices.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Mixed object/array paths are supported (e.g., <c>"users:[0]:name"</c>).
    ///             </description>
    ///         </item>
    ///     </list>
    ///     <para>
    ///         Because flattened values are strings, all reconstructed leaf values are JSON strings; the original
    ///         JSON value kinds (number, boolean) are not restored.
    ///     </para>
    /// </remarks>
    public static JsonObject Unflatten(IDictionary<string, string?> flattened, string separator = ":")
    {
        #if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(flattened);
        #else
        if (flattened is null)
            throw new ArgumentNullException(nameof(flattened));
        #endif

        var obj = new JsonObject();

        foreach (var (key, value) in flattened)
        {
            // Split the flattened key into path segments using the provided separator as delimiter
            var path = key.Split(separator);

            // Track the current object context during path traversal
            var currentObject = obj;

            // Track the current array context (null when not in an array)
            JsonArray? currentArray = null;

            // Traverse each segment of the path to build the hierarchical structure
            for (var i = 0; i < path.Length; i++)
            {
                var pathPart = path[i];

                // Process intermediate path segments (not the final value)
                if (i < path.Length - 1)
                {
                    // Handle array index notation [0], [1], etc.
                    if (TryParseArrayIndex(pathPart, out var index))
                    {
                        var array = new JsonArray();

                        if (currentArray is not null)
                        {
                            if (currentArray.Count > index)
                            {
                                currentObject = currentArray[index] as JsonObject;
                                currentArray = currentArray[index] as JsonArray;

                                continue;
                            }

                            if (i + 1 < path.Length && TryParseArrayIndex(path[i + 1], out _))
                            {
                                currentArray.Add(array);
                                currentArray = array;

                                continue;
                            }

                            var newObject = new JsonObject();
                            currentArray.Add(newObject);
                            currentObject = newObject;
                            currentArray = null;

                            continue;
                        }

                        if (currentObject![pathPart] is not null)
                        {
                            currentArray = currentObject[pathPart] as JsonArray;
                            currentObject = currentObject[pathPart] as JsonObject;

                            continue;
                        }

                        currentObject.Add(pathPart, array);
                        currentArray = array;

                        continue;
                    }

                    if (currentArray is not null)
                    {
                        var section = new JsonObject();
                        currentArray.Add(section);
                        currentObject = section;
                        currentArray = null;

                        continue;
                    }

                    if (currentObject!.ContainsKey(pathPart))
                    {
                        if (i + 1 < path.Length && TryParseArrayIndex(path[i + 1], out _))
                            currentArray = currentObject[pathPart] as JsonArray;

                        currentObject = currentObject[pathPart] as JsonObject;

                        continue;
                    }

                    if (i + 1 < path.Length && TryParseArrayIndex(path[i + 1], out _))
                    {
                        currentArray = [];
                        currentObject.Add(pathPart, currentArray);

                        continue;
                    }

                    var newSection = new JsonObject();
                    currentObject.Add(pathPart, newSection);
                    currentObject = newSection;

                    continue;
                }

                // Process the final path segment (the actual value assignment)
                if (TryParseArrayIndex(pathPart, out var finalIndex))
                {
                    if (currentArray is not null)
                    {
                        if (currentArray.Count > finalIndex)
                        {
                            currentArray[finalIndex] = value;

                            continue;
                        }

                        currentArray.Add(value);

                        continue;
                    }

                    if (currentObject![pathPart] is not null)
                    {
                        currentObject[pathPart] = value;

                        continue;
                    }

                    currentObject.Add(pathPart, value);

                    continue;
                }

                if (currentArray is not null)
                {
                    currentArray.Add(value);

                    continue;
                }

                if (currentObject!.ContainsKey(pathPart))
                {
                    currentObject[pathPart] = value;

                    continue;
                }

                currentObject.Add(pathPart, value);
            }
        }

        return obj;
    }

    /// <summary>
    ///     Parses an array index from bracket notation (e.g., "[42]") using span operations,
    ///     avoiding regex overhead and intermediate string allocations.
    /// </summary>
    /// <param name="input">The path segment to parse (e.g., "[42]").</param>
    /// <param name="index">When this method returns <c>true</c>, contains the parsed zero-based index; otherwise, 0.</param>
    /// <returns><c>true</c> if the segment is a valid bracketed array index; otherwise, <c>false</c>.</returns>
    private static bool TryParseArrayIndex(string input, out int index)
    {
        index = 0;

        if (input.Length < 3 || input[0] != '[' || input[^1] != ']')
            return false;

        var span = input.AsSpan(1, input.Length - 2);

        return int.TryParse(span, out index);
    }
}
