// <copyright file="TriviaTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class TriviaTests
{
    [TestMethod]
    public void DecodePreservesComments()
    {
        var json = """
            {
                // This is a comment
                "key": "value" /* inline comment */
            }
            """;
        var result = JsonCodec.Decode(json);
        Assert.AreEqual(json, JsonCodec.Encode(result));

        // Assert that comments are preserved in trivia
        var p = result.GetObjectValue().GetProperty("key");
        Assert.IsTrue(p.Key.LeadingTrivia.Any(t => t.Kind == JsonTriviaKind.SingleLineComment && t.Value.ToString() == "// This is a comment"));
        Assert.IsTrue(p.Value.TrailingTrivia.Any(t => t.Kind == JsonTriviaKind.MultiLineComment && t.Value.ToString() == "/* inline comment */"));
    }

    [TestMethod]
    public void DecodePreservesWhitespaces()
    {
        var json = """  {  "key"  :  "value"  }  """;
        var result = JsonCodec.Decode(json);
        Assert.AreEqual(json, JsonCodec.Encode(result));
        var obj = result.GetObjectValue();

        // since there is no newline before the key, all whitespace is in leading trivia of the object
        Assert.IsTrue(obj.LeadingTrivia.Any(t => t.Kind == JsonTriviaKind.Whitespace && t.Value.ToString().Contains("  ")));
        var p = obj.GetProperty("key");
        Assert.IsTrue(p.Key.TrailingTrivia.Last().Kind == JsonTriviaKind.Colon);
        Assert.IsTrue(p.Value.LeadingTrivia.Any(t => t.Kind == JsonTriviaKind.Whitespace && t.Value.ToString().Contains("  ")));
        Assert.IsTrue(p.Value.TrailingTrivia.Any(t => t.Kind == JsonTriviaKind.Whitespace && t.Value.ToString().Contains("  ")));
    }

    [TestMethod]
    public void DecodePreservesLineBreaks()
    {
        var json = """
        {
            "key": // key comment
               /* value comment */ { "innerKey": "innerValue" }
        }
        """;

        var result = JsonCodec.Decode(json);
        Assert.AreEqual(json, JsonCodec.Encode(result));
        var obj = result.GetObjectValue();
        var p = obj.GetProperty("key");
        Assert.IsTrue(p.Key.TrailingTrivia.Any(t => t.Kind == JsonTriviaKind.SingleLineComment && t.Value.ToString() == "// key comment"));
        Assert.IsTrue(p.Key.TrailingTrivia.Last().Kind == JsonTriviaKind.NewLine); // newline after comment should be part of key trailing trivia
        Assert.IsTrue(p.Value.LeadingTrivia.Any(t => t.Kind == JsonTriviaKind.MultiLineComment && t.Value.ToString() == "/* value comment */"));
    }
}
