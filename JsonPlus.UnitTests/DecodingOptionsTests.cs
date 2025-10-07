// <copyright file="DecodingOptionsTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using JsonPlus;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class DecodingOptionsTests
{
    [TestMethod]
    public void DecodeArrayWithAllowTrailingCommas()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: true,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        var result = JsonCodec.Decode("[1, 2, 3,]", options);

        Assert.AreEqual(JsonValueKind.JsonArray, result.Kind);
        var array = result.GetArrayValue();
        Assert.AreEqual(3, array.Count);
        Assert.AreEqual(1.0, array[0].GetDouble());
        Assert.AreEqual(2.0, array[1].GetDouble());
        Assert.AreEqual(3.0, array[2].GetDouble());
    }

    [TestMethod]
    public void DecodeObjectWithAllowTrailingCommas()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: true,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        var result = JsonCodec.Decode("""{"a": 1, "b": 2,}""", options);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        Assert.AreEqual(2, obj.Count);
        Assert.AreEqual(1.0, obj["a"].GetDouble());
        Assert.AreEqual(2.0, obj["b"].GetDouble());
    }

    [TestMethod]
    public void DecodeArrayWithoutAllowTrailingCommasFails()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("[1, 2, 3,]", options));
    }

    [TestMethod]
    public void DecodeObjectWithoutAllowTrailingCommasFails()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("""{"a": 1, "b": 2,}""", options));
    }

    [TestMethod]
    public void DecodeWithAllowSingleLineComments()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: true,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        var json = """
        {
            // This is a comment
            "value": 42
        }
        """;

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        Assert.AreEqual(42.0, obj["value"].GetDouble());
    }

    [TestMethod]
    public void DecodeManySingleLineCommentsWithAllowSingleLineComments()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: true,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        var json = """
        [
            // First comment
            1,
            // Second comment
            2,
            // Third comment
            3
        ] // end array
        """;

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonArray, result.Kind);
        var array = result.GetArrayValue();
        Assert.AreEqual(3, array.Count);
    }

    [TestMethod]
    public void DecodeWithoutAllowSingleLineCommentsFails()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        var json = """
        {
            // This comment should cause an error
            "value": 42
        }
        """;

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode(json, options));
    }

    [TestMethod]
    public void DecodeWithAllowMultiLineComments()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: true,
            MaxNestingDepth: 100);

        var json = """
        {
            /* This is a
               multi-line comment */
            "value": 42
        }
        """;

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        Assert.AreEqual(42.0, obj["value"].GetDouble());
    }

    [TestMethod]
    public void DecodeManyMultiLineCommentsWithAllowMultiLineComments()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: true,
            MaxNestingDepth: 100);

        var json = """
        [
            /* Comment before first element */
            1, /* Comment before second element
               spanning multiple lines */
            2
        /* end of array */ ]
        """;

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonArray, result.Kind);
        var array = result.GetArrayValue();
        Assert.AreEqual(2, array.Count);
    }

    [TestMethod]
    public void DecodeWithoutAllowMultiLineCommentsFails()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        var json = """
        {
            /* This comment should cause an error */
            "value": 42
        }
        """;

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode(json, options));
    }

    [TestMethod]
    public void DecodeDetectsUnterminatedMultiLineComment()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: true,
            MaxNestingDepth: 100);

        var json = """
        {
            /* Unterminated comment
            "value": 42
        }
        """;

        var ex = Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode(json, options));

        Assert.IsTrue(ex.Message.Contains("Unterminated multi-line comment"));
    }

    [TestMethod]
    public void DecodeObjectWithMaxNestingDepth()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 5);

        var json = """{"a": {"b": {"c": {"d": {"e": 42}}}}}""";

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        Assert.AreEqual(42.0, obj["a"].GetObjectValue()["b"].GetObjectValue()["c"].GetObjectValue()["d"].GetObjectValue()["e"].GetDouble());
    }

    [TestMethod]
    public void DecodeObjectFailsIfMaxNestingDepthExceeded()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 3);

        var json = """{"a": {"b": {"c": {"d": 42}}}}"""; // 4 levels deep

        var ex = Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode(json, options));

        Assert.IsTrue(ex.Message.Contains("Maximum allowed nesting depth"));
    }

    [TestMethod]
    public void DecodeArrayWithMaxNestingDepth()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 4);

        var json = """[[[[42]]]]""";

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonArray, result.Kind);
        var array = result.GetArrayValue();
        Assert.AreEqual(42.0, array[0].GetArrayValue()[0].GetArrayValue()[0].GetArrayValue()[0].GetDouble());
    }

    [TestMethod]
    public void DecodeArrayFailsIfMaxNestingDepthExceeded()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 3);

        var json = """[[[[42]]]]"""; // 4 levels deep

        var ex = Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode(json, options));

        Assert.IsTrue(ex.Message.Contains("Maximum allowed nesting depth"));
    }

    [TestMethod]
    public void DecodeMixedWithMaxNestingDepth()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 5);

        var json = """{"array": [{"nested": [{"value": 42}]}]}""";

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        var nestedValue = obj["array"].GetArrayValue()[0].GetObjectValue()["nested"].GetArrayValue()[0].GetObjectValue()["value"];
        Assert.AreEqual(42.0, nestedValue.GetDouble());
    }

    [TestMethod]
    public void DecodeComplexJsonWithCombinedOptions()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: true,
            AllowSingleLineComments: true,
            AllowMultiLineComments: true,
            MaxNestingDepth: 10);

        var json = """
        {
            // Single line comment
            "array": [
                /* Multi-line
                   comment */
                1,
                2,
                3, // Trailing comma in array
            ],
            "object": {
                "nested": true, // Trailing comma in object
            },
        }
        """;

        var result = JsonCodec.Decode(json, options);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        Assert.AreEqual(3, obj["array"].GetArrayValue().Count);
        Assert.AreEqual(true, obj["object"].GetObjectValue()["nested"].GetBoolean());
    }

    [TestMethod]
    public void DecodeWithStrictDecodingOptionsRejectsAllExtensions()
    {
        var json = """
        {
            // Comment should fail
            "value": 42,
        }
        """;

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode(json, JsonCodec.StrictDecodingOptions));
    }

    [TestMethod]
    public void DecodeWithRelaxedOptionsAllowsAllExtensions()
    {
        var json = """
        {
            // Single line comment
            /* Multi-line comment */
            "array": [1, 2, 3,],
            "value": 42,
        }
        """;

        var result = JsonCodec.Decode(json, JsonCodec.RelaxedDecodingOptions);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        Assert.AreEqual(3, obj["array"].GetArrayValue().Count);
        Assert.AreEqual(42.0, obj["value"].GetDouble());
    }

    [TestMethod]
    public void DecodeUsingDefaultOptionsAllowsAllExtensions()
    {
        var json = """
        {
            // This should work with default decode
            "value": 42,
        }
        """;

        var result = JsonCodec.Decode(json);

        Assert.AreEqual(JsonValueKind.JsonObject, result.Kind);
        var obj = result.GetObjectValue();
        Assert.AreEqual(42.0, obj["value"].GetDouble());
    }

    [TestMethod]
    public void DecodeWithMaxNestingDepthZeroThrows()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 0);

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("[]", options));

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("{}", options));
    }

    [TestMethod]
    public void DecodeWithMaxNestingDepthOneAllowsOnlyTopLevelStructures()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 1);

        // These should work (depth 1)
        var array = JsonCodec.Decode("[1, 2, 3]", options);
        Assert.AreEqual(JsonValueKind.JsonArray, array.Kind);

        var obj = JsonCodec.Decode("""{"key": "value"}""", options);
        Assert.AreEqual(JsonValueKind.JsonObject, obj.Kind);

        // These should fail (depth 2)
        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("[[]]", options));

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("""{"key": {}}""", options));
    }

    [TestMethod]
    public void DecodeEmptyArrayAndObjectWithTrailingCommaFails()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: false,
            AllowMultiLineComments: false,
            MaxNestingDepth: 100);

        // Empty structures with trailing comma should always be invalid
        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("[,]", options));

        Assert.ThrowsException<JsonParsingException>(() =>
            JsonCodec.Decode("{,}", options));
    }

    [TestMethod]
    public void DecodeCommentAtEndOfFile()
    {
        var options = new JsonDecodingOptions(
            AllowTrailingCommas: false,
            AllowSingleLineComments: true,
            AllowMultiLineComments: true,
            MaxNestingDepth: 100);

        var json1 = """42 // End comment""";
        var result1 = JsonCodec.Decode(json1, options);
        Assert.AreEqual(42.0, result1.GetDouble());

        var json2 = """42 /* End comment */""";
        var result2 = JsonCodec.Decode(json2, options);
        Assert.AreEqual(42.0, result2.GetDouble());
    }
}
