# JsonPlus

JsonPlus is a C# JSON library which preserves trivia during round-trip decoding/encoding.
The average decoding performance is around 2x slower than `System.Text.Json`.

The library supports changes of the resulting object model, including trivia (comments, whitespace, etc.),
and re-serialization back to JSON, with the rest of the formatting intact.

Below is an example of modifying a property value while preserving comments and formatting.

```csharp
public void ModifyPropertyValuePreservesTrivia()
{
    var originalJson = """
    {
        "name": "John", // User's first name
        "age": /* age property */ 20, // should be over 21
        "city": "New York"
    }
    """;

    // parse and modify JSON
    var value = JsonCodec.Decode(originalJson).SetProperty("age", new JsonNumber(22));

    var expectedJson = """
    {
        "name": "John", // User's first name
        "age": /* age property */ 22, // should be over 21
        "city": "New York"
    }
    """;

    Assert.AreEqual(expectedJson, JsonCodec.Encode(value), "Modified JSON does not match expected output.");
}
```

Additional examples can be found in the [MutationTests.cs](JsonPlus.Tests/MutationTests.cs) file.
