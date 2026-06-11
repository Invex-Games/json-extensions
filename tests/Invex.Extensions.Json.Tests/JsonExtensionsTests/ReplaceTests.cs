namespace Invex.Extensions.Json.Tests.JsonExtensionsTests;

[TestFixture]
internal sealed class ReplaceTests
{
    [Test]
    public void Replace_Replaces_SimpleKeyValue()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["age"] = 30,
        };

        // Act
        var replaced = json.ReplaceValue("name", "Jane");

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "name": "Jane",
                                        "age": 30
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(replaced, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Replace_Replaces_NestedKeyValue()
    {
        // Arrange
        var json = new JsonObject
        {
            ["user"] = new JsonObject
            {
                ["name"] = "John",
                ["details"] = new JsonObject
                {
                    ["age"] = 30,
                    ["city"] = "New York",
                },
            },
        };

        // Act
        var replaced = json.ReplaceValue("user:details:city", "Los Angeles");

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "user": {
                                          "name": "John",
                                          "details": { "age": 30, "city": "Los Angeles" }
                                        }
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(replaced, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Replace_Ignores_NonExistentKey()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["age"] = 30,
        };

        // Act
        var replaced = json.ReplaceValue("nonexistent", "value");

        // Assert
        JsonNode
            .DeepEquals(replaced, json)
            .ShouldBeTrue();
    }

    [Test]
    public void Replace_NonExistentIntermediateSegments_SetsAtNearestExistingParent()
    {
        // Arrange
        var json = new JsonObject
        {
            ["user"] = new JsonObject
            {
                ["name"] = "John",
            },
        };

        // Act
        var replaced = json.ReplaceValue("user:profile:age", "30");

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "user": { "name": "John", "age": "30" }
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(replaced, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void Replace_NestedPath_NoExistingSegments_SetsOnRootFinalKey()
    {
        // Arrange
        var json = new JsonObject
        {
            ["a"] = 1,
        };

        // Act
        var replaced = json.ReplaceValue("x:y:z", "v");

        // Assert
        var expected = JsonNode.Parse("""
                                      { "a": 1, "z": "v" }
                                      """)!;

        JsonNode
            .DeepEquals(replaced, expected)
            .ShouldBeTrue();
    }
}
