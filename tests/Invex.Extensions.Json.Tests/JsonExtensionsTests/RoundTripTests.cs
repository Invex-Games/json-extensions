namespace Invex.Extensions.Json.Tests.JsonExtensionsTests;

[TestFixture]
internal sealed class RoundTripTests
{
    [Test]
    public void RoundTrip_FlattenThenUnflatten_PreservesOriginalStructure()
    {
        // Arrange
        var original = new JsonObject
        {
            ["user"] = new JsonObject
            {
                ["name"] = "John",
                ["addresses"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["city"] = "New York",
                        ["zip"] = "10001",
                    },
                    new JsonObject
                    {
                        ["city"] = "Boston",
                        ["zip"] = "02101",
                    },
                },
                ["tags"] = new JsonArray
                {
                    "admin",
                    "user",
                },
            },
        };

        // Act
        var flattened = JsonExtensions.Flatten(original);

        #if NET8_0_OR_GREATER
        var roundTrip = JsonExtensions.Unflatten(flattened
            .Select(x => new KeyValuePair<string, string?>(x.Key, x.Value))
            .ToDictionary());
        #else
        var roundTrip = JsonExtensions.Unflatten(flattened
            .Select(x => new KeyValuePair<string, string?>(x.Key, x.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value));
        #endif

        // Assert
        JsonNode
            .DeepEquals(roundTrip, original)
            .ShouldBeTrue();
    }

    [Test]
    public void RoundTrip_WithEdgeCases_MaintainsDataIntegrity()
    {
        // Arrange - Complex structure with edge cases
        var original = new JsonObject
        {
            [""] = "empty_key",
            ["array"] = new JsonArray
            {
                null,
                "value",
                new JsonObject
                {
                    ["nested"] = true,
                },
            },
            ["special-chars"] = "hyphen_in_key",
            ["unicode"] = "🚀✨",
            ["numbers"] = new JsonObject
            {
                ["int"] = 42,
                ["float"] = 3.14159,
                ["scientific"] = 1.23e-4,
            },
        };

        // Act
        var flattened = JsonExtensions.Flatten(original);
        var reconstructed = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "": "empty_key",
                                        "array": [ null, "value", { "nested": "true" } ],
                                        "special-chars": "hyphen_in_key",
                                        "unicode": "🚀✨",
                                        "numbers": {
                                          "int": "42",
                                          "float": "3.14159",
                                          "scientific": "0.000123"
                                        }
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(reconstructed, expected)
            .ShouldBeTrue();
    }
}
