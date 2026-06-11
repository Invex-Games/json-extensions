namespace Invex.Extensions.Json.Tests.JsonExtensionsTests;

[TestFixture]
internal sealed class ReplaceManyTests
{
    [Test]
    public void Replace_ShouldUpdateJsonFile()
    {
        // Arrange
        var initialJson = JsonNode.Parse("""
                                         {
                                           "name": "OldName",
                                           "version": "1.0.0",
                                           "nullable": "1.0.0",
                                           "nested": {
                                             "key": "OldValue",
                                             "andNestedAgain:withFlat": "OldNestedFlatValue"
                                           },
                                           "nestedFlat:key": "OldFlatValue"
                                         }
                                         """)!.AsObject();

        var newValues = new Dictionary<string, string?>
        {
            { "name", "NewName" },
            { "actualName", "NewName" },
            { "version", "2.0.0" },
            { "nullable", null! },
            { "nested:key", "NewValue" },
            { "nonexistent", "ShouldBeIgnored" },
            { "nested:andNestedAgain:withFlat", "NewNestedFlatValue" },
            { "nestedFlat:key", "NewFlatValue" },
        };

        // Act
        var newNode = initialJson.ReplaceValues(newValues);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "name": "NewName",
                                        "version": "2.0.0",
                                        "nullable": null,
                                        "nested": {
                                          "key": "NewValue",
                                          "andNestedAgain:withFlat": "NewNestedFlatValue"
                                        },
                                        "nestedFlat:key": "NewFlatValue"
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(newNode, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void ReplaceMany_ShouldUpdate_ArrayElement_ByNumericPath()
    {
        // Arrange
        var json = JsonNode.Parse("""
                                  {
                                    "users": [
                                      { "name": "Alice", "age": "25" },
                                      { "name": "Bob", "age": "30" }
                                    ]
                                  }
                                  """)!.AsObject();

        var replacements = new Dictionary<string, string?>
        {
            { "users:1:name", "Robert" },
            { "users:0:age", null },
        };

        // Act
        var updated = json.ReplaceValues(replacements);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "users": [
                                          { "name": "Alice", "age": null },
                                          { "name": "Robert", "age": "30" }
                                        ]
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(updated, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void ReplaceMany_ShouldIgnore_ArrayElement_WithBracketPath()
    {
        // Arrange
        var json = JsonNode.Parse("""
                                  {
                                    "users": [
                                      { "name": "Alice" },
                                      { "name": "Bob" }
                                    ]
                                  }
                                  """)!.AsObject();

        var replacements = new Dictionary<string, string?>
        {
            { "users:[1]:name", "X" },
        };

        // Act
        var updated = json.ReplaceValues(replacements);

        // Assert
        var expected = JsonNode.Parse("""
                                      {
                                        "users": [
                                          { "name": "Alice" },
                                          { "name": "Bob" }
                                        ]
                                      }
                                      """)!;

        JsonNode
            .DeepEquals(updated, expected)
            .ShouldBeTrue();
    }

    [Test]
    public void ReplaceMany_EmptyKey_Ignored()
    {
        // Arrange
        var json = JsonNode.Parse("""
                                  { "name": "Alice" }
                                  """)!.AsObject();

        var replacements = new Dictionary<string, string?>
        {
            { string.Empty, "ignored" },
        };

        // Act
        var updated = json.ReplaceValues(replacements);

        // Assert
        JsonNode
            .DeepEquals(updated, json)
            .ShouldBeTrue();
    }
}
