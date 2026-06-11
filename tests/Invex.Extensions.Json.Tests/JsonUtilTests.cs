namespace Invex.Extensions.Json.Tests;

[TestFixture]
internal sealed class JsonUtilTests
{
    [Test]
    public void ToFlattenedJsonObject()
    {
        // Arrange
        var input = JsonNode.Parse("""
                                   {
                                     "a": {
                                       "b": {
                                         "c": "d"
                                       },
                                       "e": [1, 2, 3]
                                     },
                                     "f": true
                                   }
                                   """)!.AsObject();

        var expected = JsonNode.Parse("""
                                      {
                                        "a:b:c": "d",
                                        "a:e:0": 1,
                                        "a:e:1": 2,
                                        "a:e:2": 3,
                                        "f": true
                                      }
                                      """)!.AsObject();

        // Act
        var output = input.ToFlattenedJsonObject();

        // Assert
        output
            .ToJsonString()
            .ShouldBe(expected.ToJsonString());
    }

    [Test]
    public void ToFlattenedDictionary()
    {
        // Arrange
        var input = JsonNode.Parse("""
                                   {
                                     "a": {
                                       "b": {
                                         "c": "d"
                                       },
                                       "e": [1, 2, 3]
                                     },
                                     "f": true,
                                     "g": null
                                   }
                                   """)!.AsObject();

        var expected = new Dictionary<string, string?>
        {
            ["a:b:c"] = "d",
            ["a:e:0"] = "1",
            ["a:e:1"] = "2",
            ["a:e:2"] = "3",
            ["f"] = "true",
            ["g"] = null,
        };

        // Act
        var dict = input.ToFlattenedDictionary();

        // Assert
        dict.ShouldBe(expected);
    }

    [Test]
    public void HasNestedObjects()
    {
        // Arrange
        var input1 = JsonNode.Parse("""
                                    {
                                      "a": {
                                        "b": {
                                          "c": "d"
                                        }
                                      },
                                      "f": true
                                    }
                                    """)!.AsObject();

        var input2 = JsonNode.Parse("""
                                    {
                                      "a": "value",
                                      "f": true
                                    }
                                    """)!.AsObject();

        // Act & Assert
        input1
            .HasNestedObjects()
            .ShouldBeTrue();

        input2
            .HasNestedObjects()
            .ShouldBeFalse();
    }

    [Test]
    public void ToUnflattenedJsonObject()
    {
        // Arrange
        var input = JsonNode.Parse("""
                                   {
                                     "a:b:c": "d",
                                     "a:e:0": 1,
                                     "a:e:1": 2,
                                     "a:e:2": 3,
                                     "f": true
                                   }
                                   """)!.AsObject();

        var expected = JsonNode.Parse("""
                                      {
                                        "a": {
                                          "b": {
                                            "c": "d"
                                          },
                                          "e": [1, 2, 3]
                                        },
                                        "f": true
                                      }
                                      """)!.AsObject();

        // Act
        var output = input.ToUnflattenedJsonObject();

        // Assert
        output
            .ToJsonString()
            .ShouldBe(expected.ToJsonString());
    }
}
