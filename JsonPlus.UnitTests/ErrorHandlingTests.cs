// <copyright file="ErrorHandlingTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ErrorHandlingTests
{
    [TestMethod]
    public void DecodeInvalidJsonThrowsJsonParsingException()
    {
        Assert.ThrowsException<JsonParsingException>(() => JsonCodec.Decode("{invalid}"));
    }

    [TestMethod]
    public void DecodeInvalidJsonProvidesMeaningfulErrorMessage()
    {
        try
        {
            JsonCodec.Decode("""{"key": }""");
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Unexpected character '}'", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(9, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeNullInputThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() => JsonCodec.Decode(null!));
    }

    [TestMethod]
    public void DecodeEmptyInputThrowsUnexpectedEndOfInput()
    {
        try
        {
            JsonCodec.Decode(string.Empty);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Unexpected end of input", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(1, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeWhitespacesThrowsUnexpectedEndOfInput()
    {
        try
        {
            JsonCodec.Decode("// This is a comment");
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Unexpected end of input", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(21, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeCommentsThrowsUnexpectedEndOfInput()
    {
        try
        {
            JsonCodec.Decode("   ");
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Unexpected end of input", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(4, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeUnterminatedObjectThrows()
    {
        try
        {
            JsonCodec.Decode("""
                {
                    "key": {
                        "innerKey": "value"
                    }
                """);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Expected ',' or '}' in object", ex.Message);
            Assert.AreEqual(4, ex.Position.Line);
            Assert.AreEqual(6, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeUnterminatedArrayThrows()
    {
        try
        {
            JsonCodec.Decode("""
                [
                    1,
                    2,
                    3
                """);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Expected ',' or ']' in array", ex.Message);
            Assert.AreEqual(4, ex.Position.Line);
            Assert.AreEqual(6, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeInvalidStringThrows()
    {
        try
        {
            JsonCodec.Decode(""" "Unterminated string """);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Unterminated string literal", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(23, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeInvalidEscapeSequenceThrows()
    {
        try
        {
            JsonCodec.Decode(""" "Invalid escape: \x" """);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Invalid escape character '\\x' in string literal", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(20, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeInvalidUnicodeEscapeThrows()
    {
        try
        {
            JsonCodec.Decode(""" "Invalid unicode escape: \u12G4" """);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Invalid hex character in escape sequence", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(31, ex.Position.Column);
        }
    }

    [TestMethod]
    public void DecodeUnicodeEscapeWithMissingDigitsThrows()
    {
        try
        {
            JsonCodec.Decode(""" "Invalid unicode escape: \u123 abc" """);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Invalid hex character in escape sequence", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
            Assert.AreEqual(32, ex.Position.Column);
        }
    }

    [TestMethod]
    [DataRow("t")]
    [DataRow("f")]
    [DataRow("n")]
    [DataRow("fa")]
    [DataRow("tr")]
    [DataRow("nulls")]
    [DataRow("true1")]
    [DataRow("false1")]
    public void DecodeInvalidLiteralFails(string invalidLiteral)
    {
        try
        {
            JsonCodec.Decode(invalidLiteral);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.IsTrue(ex.Message.StartsWith("Expected", StringComparison.Ordinal));
            Assert.AreEqual(1, ex.Position.Line);
        }
    }

    [TestMethod]
    [DataRow("01")]
    [DataRow("1.")]
    [DataRow("1e")]
    [DataRow("1e+")]
    [DataRow("1e-")]
    [DataRow("1.0e")]
    [DataRow("1.0e-")]
    [DataRow("1.0e+")]
    [DataRow("1.0.0")]
    [DataRow("1a")]
    [DataRow("1e1.0")]
    [DataRow("1e1e1")]
    [DataRow("1..0")]
    public void DecodeInvalidNumberFails1(string invalidNumber)
    {
        try
        {
            JsonCodec.Decode(invalidNumber);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.IsTrue(ex.Message.StartsWith("Expected", StringComparison.Ordinal));
            Assert.AreEqual(1, ex.Position.Line);
        }
    }

    [TestMethod]
    [DataRow("--1")]
    [DataRow("-")]
    public void DecodeInvalidNumberFails2(string invalidNumber)
    {
        try
        {
            JsonCodec.Decode(invalidNumber);
            Assert.Fail("Expected JsonParsingException");
        }
        catch (JsonParsingException ex)
        {
            Assert.AreEqual("Invalid number format", ex.Message);
            Assert.AreEqual(1, ex.Position.Line);
        }
    }
}
