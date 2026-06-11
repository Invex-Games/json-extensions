namespace Invex.Extensions.Json.Tests.JsonExtensionsTests;

[TestFixture]
internal sealed class FlattenTests
{
    [Test]
    public void Flatten_JsonObject_ReturnsFlattenedList()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["age"] = 30,
            ["address"] = new JsonObject
            {
                ["street"] = "123 Main St",
                ["city"] = "Anytown",
            },
            ["phones"] = new JsonArray
            {
                "123-456-7890",
                "987-654-3210",
            },
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);
        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(6);
        flattened.ShouldContain(x => x.Key == "name" && x.Value == "John");
        flattened.ShouldContain(x => x.Key == "age" && x.Value == "30");
        flattened.ShouldContain(x => x.Key == "address:street" && x.Value == "123 Main St");
        flattened.ShouldContain(x => x.Key == "address:city" && x.Value == "Anytown");
        flattened.ShouldContain(x => x.Key == "phones:[0]" && x.Value == "123-456-7890");
        flattened.ShouldContain(x => x.Key == "phones:[1]" && x.Value == "987-654-3210");
    }

    [Test]
    public void Flatten_EmptyJsonObject_ReturnsEmptyList()
    {
        // Arrange
        var json = new JsonObject();

        // Act
        var flattened = JsonExtensions.Flatten(json);

        // Assert
        flattened.Count.ShouldBe(0);
    }

    [Test]
    public void Flatten_JsonArray_ReturnsFlattenedWithIndices()
    {
        // Arrange
        var json = new JsonArray
        {
            "first",
            "second",
            "third",
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);
        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(3);
        flattened.ShouldContain(x => x.Key == ":[0]" && x.Value == "first");
        flattened.ShouldContain(x => x.Key == ":[1]" && x.Value == "second");
        flattened.ShouldContain(x => x.Key == ":[2]" && x.Value == "third");
    }

    [Test]
    public void Flatten_NestedArrays_ReturnsCorrectStructure()
    {
        // Arrange
        var json = new JsonObject
        {
            ["matrix"] = new JsonArray
            {
                new JsonArray
                {
                    1,
                    2,
                    3,
                },
                new JsonArray
                {
                    4,
                    5,
                    6,
                },
            },
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(6);
        flattened.ShouldContain(x => x.Key == "matrix:[0]:[0]" && x.Value == "1");
        flattened.ShouldContain(x => x.Key == "matrix:[0]:[1]" && x.Value == "2");
        flattened.ShouldContain(x => x.Key == "matrix:[0]:[2]" && x.Value == "3");
        flattened.ShouldContain(x => x.Key == "matrix:[1]:[0]" && x.Value == "4");
        flattened.ShouldContain(x => x.Key == "matrix:[1]:[1]" && x.Value == "5");
        flattened.ShouldContain(x => x.Key == "matrix:[1]:[2]" && x.Value == "6");
    }

    [Test]
    public void Flatten_WithNullValues_HandlesNullsCorrectly()
    {
        // Arrange
        var json = new JsonObject
        {
            ["name"] = "John",
            ["middleName"] = null,
            ["age"] = 30,
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(3);
        flattened.ShouldContain(x => x.Key == "name" && x.Value == "John");
        flattened.ShouldContain(x => x.Key == "middleName" && x.Value == null);
        flattened.ShouldContain(x => x.Key == "age" && x.Value == "30");
    }

    [Test]
    public void Flatten_DeeplyNested_HandlesComplexStructure()
    {
        // Arrange
        var json = new JsonObject
        {
            ["level1"] = new JsonObject
            {
                ["level2"] = new JsonObject
                {
                    ["level3"] = new JsonObject
                    {
                        ["value"] = "deep",
                    },
                },
            },
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(1);
        flattened.ShouldContain(x => x.Key == "level1:level2:level3:value" && x.Value == "deep");
    }

    [Test]
    public void Flatten_ArrayOfObjects_ReturnsCorrectStructure()
    {
        // Arrange
        var json = new JsonObject
        {
            ["users"] = new JsonArray
            {
                new JsonObject
                {
                    ["name"] = "Alice",
                    ["age"] = 25,
                },
                new JsonObject
                {
                    ["name"] = "Bob",
                    ["age"] = 30,
                },
            },
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(4);
        flattened.ShouldContain(x => x.Key == "users:[0]:name" && x.Value == "Alice");
        flattened.ShouldContain(x => x.Key == "users:[0]:age" && x.Value == "25");
        flattened.ShouldContain(x => x.Key == "users:[1]:name" && x.Value == "Bob");
        flattened.ShouldContain(x => x.Key == "users:[1]:age" && x.Value == "30");
    }

    [Test]
    public void Flatten_PrimitiveTypes_HandlesAllTypes()
    {
        // Arrange
        var json = new JsonObject
        {
            ["string"] = "test",
            ["number"] = 42,
            ["decimal"] = 3.14,
            ["boolean"] = true,
            ["null"] = null,
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(5);
        flattened.ShouldContain(x => x.Key == "string" && x.Value == "test");
        flattened.ShouldContain(x => x.Key == "number" && x.Value == "42");
        flattened.ShouldContain(x => x.Key == "decimal" && x.Value == "3.14");
        flattened.ShouldContain(x => x.Key == "boolean" && x.Value == "true");
        flattened.ShouldContain(x => x.Key == "null" && x.Value == null);
    }

    [Test]
    public void Flatten_SinglePrimitiveValue_ReturnsEmptyKeyWithValue()
    {
        // Arrange
        JsonNode json = JsonValue.Create("standalone");

        // Act
        var flattened = JsonExtensions.Flatten(json);

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(1);
        flattened.ShouldContain(x => x.Key == "" && x.Value == "standalone");
    }

    [Test]
    public void Flatten_EmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var json = new JsonArray();

        // Act
        var flattened = JsonExtensions.Flatten(json);

        // Assert
        flattened.Count.ShouldBe(0);
    }

    [Test]
    public void Flatten_NullJsonNode_ThrowsArgumentNullException()
    {
        // Arrange
        JsonNode? json = null;

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => JsonExtensions.Flatten(json!));
    }

    [Test]
    public void Flatten_ExtremelyDeepNesting_HandlesWithoutStackOverflow()
    {
        // Arrange - Create 100 levels deep
        var json = new JsonObject();
        var current = json;

        for (var i = 0; i < 100; i++)
        {
            var next = new JsonObject
            {
                ["value"] = i.ToString(),
            };

            current[$"level{i}"] = next;
            current = next;
        }

        // Act
        var flattened = JsonExtensions.Flatten(json);

        // Assert
        flattened.Count.ShouldBe(100);

        flattened
            .Any(x => x.Key.Split(':')
                          .Length ==
                      101)
            .ShouldBeTrue();
    }

    [Test]
    public void Flatten_ArraysWithNullElements_PreservesNulls()
    {
        // Arrange
        var json = new JsonObject
        {
            ["items"] = new JsonArray
            {
                "first",
                null,
                "third",
                null,
            },
        };

        // Act
        var flattened = JsonExtensions.Flatten(json);

        using var _ = Assert.EnterMultipleScope();

        flattened.Count.ShouldBe(4);
        flattened.ShouldContain(x => x.Key == "items:[0]" && x.Value == "first");
        flattened.ShouldContain(x => x.Key == "items:[1]" && x.Value == null);
        flattened.ShouldContain(x => x.Key == "items:[2]" && x.Value == "third");
        flattened.ShouldContain(x => x.Key == "items:[3]" && x.Value == null);
    }
}
