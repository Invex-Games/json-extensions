namespace Invex.Extensions.Json;

/// <summary>
///     Provides extension members on <see cref="JsonObject" /> for flattening a hierarchical JSON structure into a
///     single level and reconstructing (unflattening) it again.
///     <para>Path conventions used by this class:</para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Object property segments are joined with a configurable separator (default <c>":"</c>),
///                 e.g., <c>"user:address:city"</c>.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Array elements are addressed with bare zero-based numeric segments, e.g., <c>"users:0:name"</c>.
///             </description>
///         </item>
///     </list>
///     <para>
///         Note: this differs from <see cref="JsonExtensions.Flatten(JsonNode, string)" />, which uses bracketed
///         indices such as <c>"users:[0]:name"</c>.
///     </para>
/// </summary>
[PublicAPI]
public static class JsonUtil
{
    extension(JsonObject jsonObject)
    {
        /// <summary>
        ///     Flattens this <see cref="JsonObject" /> into a new single-level <see cref="JsonObject" /> whose keys are
        ///     full paths to each leaf value.
        /// </summary>
        /// <param name="separator">
        ///     The string used to join path segments. May be more than one character. Defaults to <c>":"</c>.
        /// </param>
        /// <returns>
        ///     A new <see cref="JsonObject" /> in which:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 Each key is the separator-joined path to a leaf value (array elements use bare numeric
        ///                 segments, e.g., <c>"users:0:name"</c>).
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 Each value is a deep clone of the original leaf node, so JSON types (string, number,
        ///                 boolean) are preserved and no references are shared with the source object.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>JSON null leaves are preserved as null values.</description>
        ///         </item>
        ///     </list>
        ///     <para>Keys appear in depth-first, insertion order. The source object is not modified.</para>
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Empty nested objects and arrays produce no entries in the output, so they are not round-trippable
        ///         via <see cref="ToUnflattenedJsonObject" />.
        ///     </para>
        ///     <para>
        ///         Choose a separator that does not occur in any property name; otherwise unflattening will split
        ///         those names incorrectly.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code><![CDATA[
        /// var obj = JsonNode.Parse("""{ "user": { "name": "John", "tags": ["admin", "user"] } }""").AsObject();
        /// var flat = obj.ToFlattenedJsonObject();
        /// // {"user:name":"John","user:tags:0":"admin","user:tags:1":"user"}
        /// ]]></code>
        /// </example>
        [PublicAPI]
        public JsonObject ToFlattenedJsonObject(string separator = ":")
        {
            var output = new JsonObject();

            // Iterative DFS traversal using an explicit stack to preserve DFS order
            var stack = new Stack<(JsonNode? Node, string Path)>();

            // Seed stack with root object's properties in reverse insertion order
            var children = new List<KeyValuePair<string, JsonNode?>>();

            children.AddRange(jsonObject);

            for (var i = children.Count - 1; i >= 0; i--)
            {
                var (key, value) = (children[i].Key, children[i].Value);
                stack.Push((value, key));
            }

            while (stack.Count > 0)
            {
                var (node, path) = stack.Pop();

                if (node is null)
                {
                    output[path] = null;

                    continue;
                }

                switch (node)
                {
                    case JsonObject obj:
                    {
                        // Push properties in reverse to process them in insertion order
                        children.Clear();
                        children.AddRange(obj);

                        for (var i = children.Count - 1; i >= 0; i--)
                        {
                            var (key, value) = (children[i].Key, children[i].Value);
                            stack.Push((value, string.Concat(path, separator, key)));
                        }

                        break;
                    }

                    case JsonArray arr:
                    {
                        // Push elements in reverse index order so 0..N are processed in order
                        for (var i = arr.Count - 1; i >= 0; i--)
                            stack.Push((arr[i], string.Concat(path, separator, i.ToString())));

                        break;
                    }

                    default:
                    {
                        // Leaf value: clone to keep original types and avoid sharing references
                        output[path] = node.DeepClone();

                        break;
                    }
                }
            }

            return output;
        }

        /// <summary>
        ///     Flattens this <see cref="JsonObject" /> into a <see cref="Dictionary{TKey,TValue}" /> mapping each leaf
        ///     path to its string representation.
        /// </summary>
        /// <param name="separator">
        ///     The string used to join path segments. May be more than one character. Defaults to <c>":"</c>.
        /// </param>
        /// <returns>
        ///     A dictionary in which:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 Each key is the separator-joined path to a leaf value (array elements use bare numeric
        ///                 segments, e.g., <c>"users:0:name"</c>).
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 String values are stored verbatim; numbers use their raw JSON text; booleans become
        ///                 <c>"true"</c>/<c>"false"</c>; JSON nulls become <c>null</c> entries.
        ///             </description>
        ///         </item>
        ///     </list>
        ///     <para>Keys appear in depth-first, insertion order. The source object is not modified.</para>
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Because all values are converted to strings, the original JSON value kinds (number vs. string vs.
        ///         boolean) are not recoverable from the result. Use <see cref="ToFlattenedJsonObject" /> when type
        ///         fidelity matters.
        ///     </para>
        ///     <para>Empty nested objects and arrays produce no entries in the output.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if a leaf node reports an unexpected <see cref="JsonValueKind" /> (e.g.,
        ///     <see cref="JsonValueKind.Undefined" />); this is not expected for well-formed JSON.
        /// </exception>
        /// <example>
        ///     <code><![CDATA[
        /// var obj = JsonNode.Parse("""{ "user": { "age": 42, "active": true } }""").AsObject();
        /// var flat = obj.ToFlattenedDictionary();
        /// // { ["user:age"] = "42", ["user:active"] = "true" }
        /// ]]></code>
        /// </example>
        [PublicAPI]
        public Dictionary<string, string?> ToFlattenedDictionary(string separator = ":")
        {
            var output = new Dictionary<string, string?>();

            // Iterative DFS traversal using an explicit stack to preserve DFS order
            var stack = new Stack<(JsonNode? Node, string Path)>();

            // Seed stack with root object's properties in reverse insertion order
            var children = new List<KeyValuePair<string, JsonNode?>>();
            children.AddRange(jsonObject);

            for (var i = children.Count - 1; i >= 0; i--)
            {
                var (key, value) = (children[i].Key, children[i].Value);
                stack.Push((value, key));
            }

            while (stack.Count > 0)
            {
                var (node, path) = stack.Pop();

                if (node is null)
                {
                    // Note: assigning null to Dictionary<string,string> will produce a nullable warning in nullable-enabled contexts
                    // but is allowed at runtime and preserves parity with Flatten(JsonObject) which retains nulls.
                    output[path] = null!;

                    continue;
                }

                switch (node)
                {
                    case JsonObject obj:
                    {
                        // Push properties in reverse to process them in insertion order
                        children.Clear();
                        children.AddRange(obj);

                        for (var i = children.Count - 1; i >= 0; i--)
                        {
                            var (key, value) = (children[i].Key, children[i].Value);
                            stack.Push((value, string.Concat(path, separator, key)));
                        }

                        break;
                    }

                    case JsonArray arr:
                    {
                        // Push elements in reverse index order so 0..N are processed in order
                        for (var i = arr.Count - 1; i >= 0; i--)
                            stack.Push((arr[i], string.Concat(path, separator, i.ToString())));

                        break;
                    }

                    default:
                    {
                        output[path] = node.GetValueKind() switch
                        {
                            JsonValueKind.Undefined => throw new InvalidOperationException(
                                "Undefined JsonNode cannot be converted to string"),
                            JsonValueKind.Object => throw new InvalidOperationException(
                                "Object JsonNode cannot be converted to string"),
                            JsonValueKind.Array => throw new InvalidOperationException(
                                "Array JsonNode cannot be converted to string"),
                            JsonValueKind.String => node.ToString(),
                            JsonValueKind.Number => node.ToJsonString(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            JsonValueKind.Null => null,
                            _ => throw new InvalidOperationException("Unknown JsonNode kind"),
                        };

                        break;
                    }
                }
            }

            return output;
        }

        /// <summary>
        ///     Determines whether this <see cref="JsonObject" /> contains any nested containers, i.e., whether any
        ///     direct property value is a <see cref="JsonObject" /> or <see cref="JsonArray" />.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if at least one direct property value is an object or array; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        ///     Only the top level is inspected; this is a quick check for whether the object is already "flat".
        /// </remarks>
        [PublicAPI]
        public bool HasNestedObjects()
        {
            foreach (var child in jsonObject)
                if (child.Value is JsonObject or JsonArray)
                    return true;

            return false;
        }

        /// <summary>
        ///     Reconstructs a hierarchical <see cref="JsonObject" /> from a flattened object whose keys are
        ///     separator-joined paths, reversing <see cref="ToFlattenedJsonObject" />.
        /// </summary>
        /// <param name="separator">
        ///     The string used to split keys into path segments. Must match the separator used when flattening.
        ///     May be more than one character. Defaults to <c>":"</c>.
        /// </param>
        /// <returns>
        ///     A new <see cref="JsonObject" /> with nested objects and arrays rebuilt from the paths:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 A purely numeric segment causes its parent container to be created as a
        ///                 <see cref="JsonArray" />; any other segment creates a <see cref="JsonObject" />.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 Leaf values are deep clones of the source values, so no references are shared.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 Sparse array indices are padded with nulls (e.g., a single key <c>"items:2"</c> yields
        ///                 <c>[null, null, value]</c>).
        ///             </description>
        ///         </item>
        ///     </list>
        ///     <para>The source object is not modified.</para>
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Property names that are purely numeric are indistinguishable from array indices and will be
        ///         reconstructed as array elements.
        ///     </para>
        ///     <para>If the same path is supplied more than once, the last value wins.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when a non-numeric segment is used where an array index is required (i.e., the structure
        ///     implied by one key conflicts with an array created for another key).
        /// </exception>
        /// <example>
        ///     <code><![CDATA[
        /// var flat = new JsonObject
        /// {
        ///     ["user:name"] = "John",
        ///     ["user:tags:0"] = "admin",
        ///     ["user:tags:1"] = "user",
        /// };
        /// var obj = flat.ToUnflattenedJsonObject();
        /// // {"user":{"name":"John","tags":["admin","user"]}}
        /// ]]></code>
        /// </example>
        [PublicAPI]
        public JsonObject ToUnflattenedJsonObject(string separator = ":")
        {
            // Root of the reconstructed tree
            var root = new JsonObject();

            foreach (var (path, value) in jsonObject)
            {
                // Split by the provided separator (can be multi-character)
                var segments = path.Split([separator], StringSplitOptions.None);

                if (segments.Length == 0)
                    continue;

                JsonNode current = root;

                // Traverse/create intermediate containers
                for (var i = 0; i < segments.Length - 1; i++)
                {
                    var seg = segments[i];
                    var nextSeg = segments[i + 1];
                    var nextIsIndex = int.TryParse(nextSeg, out _);

                    switch (current)
                    {
                        case JsonObject obj:
                        {
                            var child = obj[seg];

                            if (child is null)
                            {
                                child = nextIsIndex
                                    ? new JsonArray()
                                    : new JsonObject();

                                obj[seg] = child;
                            }

                            current = child;

                            break;
                        }

                        case JsonArray arr:
                        {
                            if (!int.TryParse(seg, out var index))
                                throw new InvalidOperationException($"Segment '{seg}' is not a valid array index.");

                            // Ensure capacity
                            while (arr.Count <= index)
                                arr.Add(null);

                            var child = arr[index];

                            if (child is null)
                            {
                                child = nextIsIndex
                                    ? new JsonArray()
                                    : new JsonObject();

                                arr[index] = child;
                            }

                            current = child;

                            break;
                        }

                        default:
                        {
                            // We hit a primitive where a container is expected; replace it with an object/array.
                            // This scenario shouldn't occur with valid flattened input, but we guard defensively.
                            JsonNode replacement = nextIsIndex
                                ? new JsonArray()
                                : new JsonObject();

                            // We cannot directly replace without knowing the parent; however, with valid input this won't happen.
                            // So just set current to the replacement to continue building (structure will be reachable from root only in valid inputs).
                            current = replacement;

                            break;
                        }
                    }
                }

                // Assign the final value at the last segment
                var lastSeg = segments[^1];

                switch (current)
                {
                    case JsonObject lastObj:
                        lastObj[lastSeg] = value?.DeepClone();

                        break;

                    case JsonArray lastArr:
                    {
                        if (!int.TryParse(lastSeg, out var index))
                            throw new InvalidOperationException($"Segment '{lastSeg}' is not a valid array index.");

                        while (lastArr.Count <= index)
                            lastArr.Add(null);

                        lastArr[index] = value?.DeepClone();

                        break;
                    }

                    default:
                        // As above, this should not occur for valid flattened input
                        throw new InvalidOperationException(
                            "Unexpected non-container node while assigning final segment.");
                }
            }

            return root;
        }
    }
}
