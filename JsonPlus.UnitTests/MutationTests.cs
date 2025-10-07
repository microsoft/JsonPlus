// <copyright file="MutationTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MutationTests
{
    [TestMethod]
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

    [TestMethod]
    public void AddPropertyPreservesTrivia()
    {
        var originalJson = """
        {
            "firstName": "Alice",
            "lastName": "Smith",
            "email": "alice@wonderland.com" // email your fantasy trips
        }
        """;

        // parse and modify JSON
        // the methods automatically copy indentation from the previous property
        var jsonObject = JsonCodec.Decode(originalJson)
            .AddProperty("phone", new JsonString("+1-555-0123"))
            .InsertProperty(2, new JsonProperty("address", new JsonString("123 Fantasy Rd")));

        var expectedJson = """
        {
            "firstName": "Alice",
            "lastName": "Smith",
            "address": "123 Fantasy Rd",
            "email": "alice@wonderland.com", // email your fantasy trips
            "phone": "+1-555-0123"
        }
        """;

        Assert.AreEqual(expectedJson, JsonCodec.Encode(jsonObject), "Modified JSON does not match expected output.");
    }

    [TestMethod]
    public void AddArrayItemPreservesTrivia()
    {
        var originalJson = """
        [
            "apple", // First fruit
            "banana", // Second fruit
            "cherry" // Third fruit
        ]
        """;

        // parse and modify
        // the Add/AddItem methods automatically handle trivia preservation and comma insertion
        var value = JsonCodec.Decode(originalJson).AddItem(new JsonString("date"));

        var expectedJson = """
        [
            "apple", // First fruit
            "banana", // Second fruit
            "cherry", // Third fruit
            "date"
        ]
        """;

        Assert.AreEqual(expectedJson, JsonCodec.Encode(value), "Modified JSON does not match expected output.");
    }

    [TestMethod]
    public void ModifyTriviaPreservesFormatting()
    {
        var originalJson = """
        {
            "status": "don't panic", // important status
            "answer": 42,
        }
        """;

        // decode
        var value = JsonCodec.Decode(originalJson);

        // add a comment, just after the comma
        value.GetPropertyValue("answer").TrailingTrivia
            .Insert(1, new JsonTrivia(JsonTriviaKind.SingleLineComment, " // the answer to life, universe, and everything"));

        var expectedJson = """
        {
            "status": "don't panic", // important status
            "answer": 42, // the answer to life, universe, and everything
        }
        """;

        Assert.AreEqual(expectedJson, JsonCodec.Encode(value), "Modified JSON does not match expected output.");
    }
}
