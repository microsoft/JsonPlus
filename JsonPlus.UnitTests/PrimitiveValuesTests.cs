// <copyright file="PrimitiveValuesTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PrimitiveValuesTests
{
    [TestMethod]
    public void DecodeNullReturnsJsonNull()
    {
        // Test parsing null values
        var result = JsonCodec.Decode("null");
        Assert.AreEqual(JsonValueKind.JsonNull, result.Kind);
    }

    [TestMethod]
    public void DecodeTrueValueReturnsTrue()
    {
        var result = JsonCodec.Decode("true");
        Assert.AreEqual(JsonValueKind.JsonBoolean, result.Kind);
        Assert.AreEqual(true, result.GetBoolean());
    }

    [TestMethod]
    public void DecodeFalseValueReturnsFalse()
    {
        var result = JsonCodec.Decode("false");
        Assert.AreEqual(JsonValueKind.JsonBoolean, result.Kind);
        Assert.AreEqual(false, result.GetBoolean());
    }

    [TestMethod]
    public void DecodeValidIntegerReturnsJsonNumber()
    {
        var result = JsonCodec.Decode("42");
        Assert.AreEqual(JsonValueKind.JsonNumber, result.Kind);
        Assert.AreEqual(42.0, result.GetDouble());
    }

    [TestMethod]
    public void DecodeStringReturnsJsonString()
    {
        var result = JsonCodec.Decode(""" "hello" """);
        Assert.AreEqual(JsonValueKind.JsonString, result.Kind);
        Assert.AreEqual("hello", result.GetString());
    }

    [TestMethod]
    public void DecodeLargeNumbersHandlesEdgeCases()
    {
        var result = JsonCodec.Decode("1.7976931348623157E+308");
        Assert.AreEqual(JsonValueKind.JsonNumber, result.Kind);
    }

    [TestMethod]
    [DataRow("null", JsonValueKind.JsonNull)]
    [DataRow("true", JsonValueKind.JsonBoolean)]
    [DataRow("42", JsonValueKind.JsonNumber)]
    [DataRow("3.1415", JsonValueKind.JsonNumber)]
    [DataRow("1.2e5", JsonValueKind.JsonNumber)]
    [DataRow("[]", JsonValueKind.JsonArray)]
    [DataRow("{}", JsonValueKind.JsonObject)]
    [DataRow(""" "text" """, JsonValueKind.JsonString)]
    public void DecodeValueReturnsCorrectKind(string json, JsonValueKind expected)
    {
        var result = JsonCodec.Decode(json);
        Assert.AreEqual(expected, result.Kind);
    }
}
