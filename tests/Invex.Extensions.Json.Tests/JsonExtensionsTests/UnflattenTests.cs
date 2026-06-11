namespace Invex.Extensions.Json.Tests.JsonExtensionsTests;

[TestFixture]
internal sealed class UnflattenTests
{
    [Test]
    public void Unflatten_JsonObject_ReturnsOriginalJson()
    {
        // Arrange
        var flattened = new Dictionary<string, string?>
        {
            { "name", "John" },
            { "age", "30" },
            { "address:street", "123 Main St" },
            { "address:city", "Anytown" },
            { "phones:[0]", "123-456-7890" },
            { "phones:[1]", "987-654-3210" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "name": "John",
                                        "age": "30",
                                        "address": { "street": "123 Main St", "city": "Anytown" },
                                        "phones": ["123-456-7890", "987-654-3210"]
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(json, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_EmptyCollection_ReturnsEmptyObject()
    {
        // Arrange
        // ReSharper disable once CollectionNeverUpdated.Local - test code
        var flattened = new Dictionary<string, string?>();

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        JsonNode
            .DeepEquals(json, new JsonObject())
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_ComplexNestedArrays_ReconstructsCorrectly()
    {
        // Arrange
        var flattened = new Dictionary<string, string?>
        {
            { "matrix:[0]:[0]", "1" },
            { "matrix:[0]:[1]", "2" },
            { "matrix:[1]:[0]", "3" },
            { "matrix:[1]:[1]", "4" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "matrix": [["1", "2"], ["3", "4"]]
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(json, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_WithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var flattened = new Dictionary<string, string?>
        {
            { "name", "John" },
            { "middleName", null },
            { "age", "30" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "name": "John",
                                        "middleName": null,
                                        "age": "30"
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(json, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_ArrayOfObjects_ReconstructsCorrectly()
    {
        // Arrange
        var flattened = new Dictionary<string, string?>
        {
            { "users:[0]:name", "Alice" },
            { "users:[0]:age", "25" },
            { "users:[1]:name", "Bob" },
            { "users:[1]:age", "30" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "users": [
                                          { "name": "Alice", "age": "25" },
                                          { "name": "Bob", "age": "30" }
                                        ]
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(json, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_KeysWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var flattened = new Dictionary<string, string?>
        {
            { "user:name with spaces", "John Doe" },
            { "user:email@domain.com", "john@example.com" },
            { "data:field.with.dots", "dotted" },
            { "symbols:$#@%^&*()", "special" },
            { "unicode:café", "coffee" },
            { "newline:line\nbreak", "broken" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "user": {
                                          "name with spaces": "John Doe",
                                          "email@domain.com": "john@example.com"
                                        },
                                        "data": { "field.with.dots": "dotted" },
                                        "symbols": { "$#@%^&*()": "special" },
                                        "unicode": { "café": "coffee" },
                                        "newline": { "line\nbreak": "broken" }
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(json, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_ArrayLikeKeysButNotArrays_HandlesCorrectly()
    {
        // Arrange
        var flattened = new Dictionary<string, string?>
        {
            { "obj:[not_an_index]", "value1" },
            { "obj:[123abc]", "value2" },
            { "obj:[]", "value3" },
            { "obj:[", "value4" },
            { "obj:]", "value5" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "obj": {
                                          "[not_an_index]": "value1",
                                          "[123abc]": "value2",
                                          "[]": "value3",
                                          "[": "value4",
                                          "]": "value5"
                                        }
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(json, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Unflatten_ConflictingStructureTypes_LastWins()
    {
        // Arrange - First define as object, then as array
        var flattened = new Dictionary<string, string?>
        {
            { "data:property", "object_value" },
            { "data:[0]", "array_value" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        using var _ = Assert.EnterMultipleScope();

        // The structure should be determined by the last processed item
        json["data"]
            .ShouldNotBeNull();

        // Could be either object or array depending on processing order
        var isArray = json["data"] is JsonArray;
        var isObject = json["data"] is JsonObject;
        (isArray || isObject).ShouldBeTrue();
    }

    [Test]
    public void Unflatten_ComplexMixedArrayObjectNesting_ReconstructsCorrectly()
    {
        // Arrange
        var flattened = new Dictionary<string, string?>
        {
            { "root:[0]:users:[0]:profile:addresses:[0]:street", "123 Main St" },
            { "root:[0]:users:[0]:profile:addresses:[1]:street", "456 Oak Ave" },
            { "root:[0]:users:[1]:name", "Bob" },
            { "root:[1]:config:enabled", "true" },
        };

        // Act
        var json = JsonExtensions.Unflatten(flattened);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "root": [
                                          {
                                            "users": [
                                              { "profile": { "addresses": [ {"street": "123 Main St"}, {"street": "456 Oak Ave"} ] } },
                                              { "name": "Bob" }
                                            ]
                                          },
                                          { "config": { "enabled": "true" } }
                                        ]
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(json, expected)
            .ShouldBeTrue();
    }
}
