// <copyright file="MergeTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MergeTests
{
    [TestMethod]
    public void MergeInPlaceSimpleObjectProperties()
    {
        var baseJson = """{"a": 1, "b": 2}""";
        var overrideJson = """{"b": 3, "c": 4}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);

        var resultObj = baseValue.GetObjectValue();
        Assert.AreEqual(1, resultObj["a"].GetDouble(), "Property 'a' should remain unchanged");
        Assert.AreEqual(3, resultObj["b"].GetDouble(), "Property 'b' should be overridden");
        Assert.AreEqual(4, resultObj["c"].GetDouble(), "Property 'c' should be added");
    }

    [TestMethod]
    public void MergeSimpleObjectProperties()
    {
        var baseJson = """{"a": 1, "b": 2}""";
        var overrideJson = """{"b": 3, "c": 4}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue);

        Assert.AreNotSame(baseValue, result, "Merge should return a new instance");

        var resultObj = result.GetObjectValue();
        Assert.AreEqual(1, resultObj["a"].GetDouble(), "Property 'a' should remain unchanged");
        Assert.AreEqual(3, resultObj["b"].GetDouble(), "Property 'b' should be overridden");
        Assert.AreEqual(4, resultObj["c"].GetDouble(), "Property 'c' should be added");

        // Verify original is unchanged
        var baseObj = baseValue.GetObjectValue();
        Assert.AreEqual(2, baseObj.Count, "Base object should remain unchanged");
        Assert.AreEqual(2, baseObj["b"].GetDouble(), "Base object property 'b' should remain unchanged");
    }

    [TestMethod]
    public void MergeInPlaceNestedObjects()
    {
        var baseJson = """
        {
            "user": {
                "name": "John",
                "age": 30
            },
            "settings": {
                "theme": "dark"
            }
        }
        """;

        var overrideJson = """
        {
            "user": {
                "age": 31,
                "city": "New York"
            },
            "settings": {
                "language": "en"
            }
        }
        """;

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var resultObj = baseValue.GetObjectValue();

        var user = resultObj["user"].GetObjectValue();
        Assert.AreEqual("John", user["name"].GetString(), "Nested property 'name' should remain unchanged");
        Assert.AreEqual(31, user["age"].GetDouble(), "Nested property 'age' should be overridden");
        Assert.AreEqual("New York", user["city"].GetString(), "Nested property 'city' should be added");

        var settings = resultObj["settings"].GetObjectValue();
        Assert.AreEqual("dark", settings["theme"].GetString(), "Nested property 'theme' should remain unchanged");
        Assert.AreEqual("en", settings["language"].GetString(), "Nested property 'language' should be added");
    }

    [TestMethod]
    public void MergeInPlaceDeeplyNestedObjects()
    {
        var baseJson = """
        {
            "level1": {
                "level2": {
                    "level3": {
                        "value": "original"
                    }
                }
            }
        }
        """;

        var overrideJson = """
        {
            "level1": {
                "level2": {
                    "level3": {
                        "value": "updated",
                        "newValue": "added"
                    }
                }
            }
        }
        """;

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var level3 = baseValue.GetObjectValue()["level1"].GetObjectValue()["level2"].GetObjectValue()["level3"].GetObjectValue();

        Assert.AreEqual("updated", level3["value"].GetString(), "Deeply nested property should be overridden");
        Assert.AreEqual("added", level3["newValue"].GetString(), "Deeply nested property should be added");
    }

    [TestMethod]
    public void MergeInPlaceArrayReplaceMode()
    {
        var baseJson = """{"items": [1, 2, 3]}""";
        var overrideJson = """{"items": [4, 5]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Replace);
        var items = baseValue.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(2, items.Count, "Array should be replaced");
        Assert.AreEqual(4, items[0].GetDouble(), "First item should be from override");
        Assert.AreEqual(5, items[1].GetDouble(), "Second item should be from override");
    }

    [TestMethod]
    public void MergeInPlaceArrayUnionMode()
    {
        var baseJson = """{"items": [1, 2, 3]}""";
        var overrideJson = """{"items": [3, 4, 5]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Union);
        var items = baseValue.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(5, items.Count, "Array should contain union of values");
        Assert.AreEqual(1, items[0].GetDouble());
        Assert.AreEqual(2, items[1].GetDouble());
        Assert.AreEqual(3, items[2].GetDouble());
        Assert.AreEqual(4, items[3].GetDouble());
        Assert.AreEqual(5, items[4].GetDouble());
    }

    [TestMethod]
    public void MergeArrayUnionModeDoesNotModifyOriginal()
    {
        var baseJson = """{"items": [1, 2, 3]}""";
        var overrideJson = """{"items": [3, 4, 5]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Union);
        var items = result.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(5, items.Count, "Result array should contain union of values");

        // Verify original is unchanged
        var baseItems = baseValue.GetObjectValue()["items"].GetArrayValue();
        Assert.AreEqual(3, baseItems.Count, "Base array should remain unchanged");
    }

    [TestMethod]
    public void MergeInPlaceArrayWithObjects()
    {
        var baseJson = """{"items": [{"id": 1, "name": "a"}, {"id": 2, "name": "b"}]}""";
        var overrideJson = """{"items": [{"id": 3, "name": "c"}]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Replace);
        var items = baseValue.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(1, items.Count, "Array should be replaced");
        Assert.AreEqual(3, items[0].GetObjectValue()["id"].GetDouble());
    }

    [TestMethod]
    public void MergeInPlacePrimitiveTypeChange()
    {
        var baseJson = """{"value": 123}""";
        var overrideJson = """{"value": "text"}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var value = baseValue.GetObjectValue()["value"];

        Assert.AreEqual(JsonValueKind.JsonString, value.Kind, "Value type should change to string");
        Assert.AreEqual("text", value.GetString(), "Value should be replaced");
    }

    [TestMethod]
    public void MergeInPlaceObjectToArrayTypeChange()
    {
        var baseJson = """{"data": {"key": "value"}}""";
        var overrideJson = """{"data": [1, 2, 3]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var data = baseValue.GetObjectValue()["data"];

        Assert.AreEqual(JsonValueKind.JsonArray, data.Kind, "Value type should change to array");
        Assert.AreEqual(3, data.GetArrayValue().Count, "Array should have correct length");
    }

    [TestMethod]
    public void MergeInPlaceNullValues()
    {
        var baseJson = """{"a": 1, "b": null}""";
        var overrideJson = """{"b": 2, "c": null}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var resultObj = baseValue.GetObjectValue();

        Assert.AreEqual(1, resultObj["a"].GetDouble());
        Assert.AreEqual(2, resultObj["b"].GetDouble(), "Null should be replaced with number");
        Assert.AreEqual(JsonValueKind.JsonNull, resultObj["c"].Kind, "Null should be added");
    }

    [TestMethod]
    public void MergeInPlaceBooleanValues()
    {
        var baseJson = """{"flag1": true, "flag2": false}""";
        var overrideJson = """{"flag2": true, "flag3": false}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var resultObj = baseValue.GetObjectValue();

        Assert.IsTrue(resultObj["flag1"].GetBoolean());
        Assert.IsTrue(resultObj["flag2"].GetBoolean(), "Boolean should be overridden");
        Assert.IsFalse(resultObj["flag3"].GetBoolean(), "Boolean should be added");
    }

    [TestMethod]
    public void MergeInPlaceEmptyObjects()
    {
        var baseJson = """{}""";
        var overrideJson = """{"key": "value"}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var resultObj = baseValue.GetObjectValue();

        Assert.AreEqual(1, resultObj.Count);
        Assert.AreEqual("value", resultObj["key"].GetString());
    }

    [TestMethod]
    public void MergeInPlaceEmptyOverride()
    {
        var baseJson = """{"key": "value"}""";
        var overrideJson = """{}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var resultObj = baseValue.GetObjectValue();

        Assert.AreEqual(1, resultObj.Count);
        Assert.AreEqual("value", resultObj["key"].GetString(), "Base value should remain unchanged");
    }

    [TestMethod]
    public void MergeInPlaceEmptyArrays()
    {
        var baseJson = """{"items": []}""";
        var overrideJson = """{"items": [1, 2]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Replace);
        var items = baseValue.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(2, items.Count);
    }

    [TestMethod]
    public void MergeComplexScenario()
    {
        var baseJson = """
        {
            "config": {
                "server": {
                    "host": "localhost",
                    "port": 8080
                },
                "features": ["auth", "logging"],
                "settings": {
                    "debug": true
                }
            },
            "version": "1.0.0"
        }
        """;

        var overrideJson = """
        {
            "config": {
                "server": {
                    "port": 9090,
                    "ssl": true
                },
                "features": ["caching"],
                "database": {
                    "type": "postgres"
                }
            },
            "build": "12345"
        }
        """;

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Union);
        var resultObj = result.GetObjectValue();

        // Check server config
        var server = resultObj["config"].GetObjectValue()["server"].GetObjectValue();
        Assert.AreEqual("localhost", server["host"].GetString(), "Host should remain from base");
        Assert.AreEqual(9090, server["port"].GetDouble(), "Port should be overridden");
        Assert.IsTrue(server["ssl"].GetBoolean(), "SSL should be added");

        // Check features array (union mode)
        var features = resultObj["config"].GetObjectValue()["features"].GetArrayValue();
        Assert.AreEqual(3, features.Count, "Features should be union of both arrays");

        // Check settings
        var settings = resultObj["config"].GetObjectValue()["settings"].GetObjectValue();
        Assert.IsTrue(settings["debug"].GetBoolean(), "Debug setting should remain");

        // Check database
        var database = resultObj["config"].GetObjectValue()["database"].GetObjectValue();
        Assert.AreEqual("postgres", database["type"].GetString(), "Database should be added");

        // Check root level properties
        Assert.AreEqual("1.0.0", resultObj["version"].GetString(), "Version should remain");
        Assert.AreEqual("12345", resultObj["build"].GetString(), "Build should be added");

        // Verify original is unchanged
        var baseObj = baseValue.GetObjectValue();
        Assert.IsFalse(baseObj.ContainsKey("build"), "Base should not have build property");
    }

    [TestMethod]
    public void MergeInPlaceStringValues()
    {
        var baseJson = """{"text": "hello"}""";
        var overrideJson = """{"text": "world"}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);

        Assert.AreEqual("world", baseValue.GetObjectValue()["text"].GetString());
    }

    [TestMethod]
    public void MergeInPlaceNumberValues()
    {
        var baseJson = """{"count": 10, "price": 19.99}""";
        var overrideJson = """{"count": 20}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue);
        var resultObj = baseValue.GetObjectValue();

        Assert.AreEqual(20, resultObj["count"].GetDouble());
        Assert.AreEqual(19.99, resultObj["price"].GetDouble());
    }

    [TestMethod]
    [ExpectedException(typeof(System.ArgumentNullException))]
    public void MergeInPlaceThrowsOnNullBase()
    {
        var overrideValue = JsonCodec.Decode("""{"key": "value"}""");
        JsonCodec.MergeInPlace(null!, overrideValue);
    }

    [TestMethod]
    [ExpectedException(typeof(System.ArgumentNullException))]
    public void MergeInPlaceThrowsOnNullOverride()
    {
        var baseValue = JsonCodec.Decode("""{"key": "value"}""");
        JsonCodec.MergeInPlace(baseValue, null!);
    }

    [TestMethod]
    [ExpectedException(typeof(System.ArgumentNullException))]
    public void MergeThrowsOnNullBase()
    {
        var overrideValue = JsonCodec.Decode("""{"key": "value"}""");
        JsonCodec.Merge(null!, overrideValue);
    }

    [TestMethod]
    [ExpectedException(typeof(System.ArgumentNullException))]
    public void MergeThrowsOnNullOverride()
    {
        var baseValue = JsonCodec.Decode("""{"key": "value"}""");
        JsonCodec.Merge(baseValue, null!);
    }

    [TestMethod]
    public void MergeInPlaceArrayUnionWithDifferentTypes()
    {
        var baseJson = """{"mixed": [1, "two", true]}""";
        var overrideJson = """{"mixed": [1, "three", false]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Union);
        var mixed = baseValue.GetObjectValue()["mixed"].GetArrayValue();

        // Should have: 1, "two", true, "three", false (1 is duplicate, not added again)
        Assert.AreEqual(5, mixed.Count, "Array should contain unique values");
    }

    [TestMethod]
    public void MergePreservesIndependence()
    {
        var baseJson = """{"user": {"name": "John"}}""";
        var overrideJson = """{"user": {"age": 30}}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue);

        // Modify result
        result.GetObjectValue()["user"].GetObjectValue().SetProperty("name", new JsonString("Jane"));

        // Verify base is unchanged
        var baseName = baseValue.GetObjectValue()["user"].GetObjectValue()["name"].GetString();
        Assert.AreEqual("John", baseName, "Modifying merged result should not affect base");
    }

    [TestMethod]
    public void MergeInPlaceTopLevelPrimitiveValues()
    {
        var baseJson = """123""";
        var overrideJson = """456""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        Assert.ThrowsException<InvalidOperationException>(() =>
            JsonCodec.MergeInPlace(baseValue, overrideValue));
    }

    [TestMethod]
    public void MergeInPlaceTopLevelArrays()
    {
        var baseJson = """[1, 2, 3]""";
        var overrideJson = """[4, 5]""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Replace);
        var array = baseValue.GetArrayValue();

        Assert.AreEqual(2, array.Count);
        Assert.AreEqual(4, array[0].GetDouble());
        Assert.AreEqual(5, array[1].GetDouble());
    }

    [TestMethod]
    public void MergeInPlaceTopLevelArraysUnion()
    {
        var baseJson = """[1, 2, 3]""";
        var overrideJson = """[3, 4, 5]""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var bv3 = baseValue.GetArrayValue()[2];
        var ov3 = overrideValue.GetArrayValue()[0];
        var eqr = bv3.Equals(ov3);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.Union);
        var array = baseValue.GetArrayValue();

        Assert.AreEqual(5, array.Count);
        Assert.AreEqual(1, array[0].GetDouble());
        Assert.AreEqual(2, array[1].GetDouble());
        Assert.AreEqual(3, array[2].GetDouble());
        Assert.AreEqual(4, array[3].GetDouble());
        Assert.AreEqual(5, array[4].GetDouble());
    }

    [TestMethod]
    public void MergeInPlaceArrayIndexBasedModeSameLength()
    {
        var baseJson = """[1, 2, 3]""";
        var overrideJson = """[10, 20, 30]""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = baseValue.GetArrayValue();

        // Index-based merge on top-level arrays works correctly
        Assert.AreEqual(3, items.Count, "Array should have same length");
        Assert.AreEqual(10, items[0].GetDouble(), "Index 0 should be replaced");
        Assert.AreEqual(20, items[1].GetDouble(), "Index 1 should be replaced");
        Assert.AreEqual(30, items[2].GetDouble(), "Index 2 should be replaced");
    }

    [TestMethod]
    public void MergeInPlaceArrayIndexBasedModeOverrideLonger()
    {
        var baseJson = """[1, 2]""";
        var overrideJson = """[10, 20, 30, 40]""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = baseValue.GetArrayValue();

        Assert.AreEqual(4, items.Count, "Array should extend to override length");
        Assert.AreEqual(10, items[0].GetDouble(), "Index 0 should be replaced");
        Assert.AreEqual(20, items[1].GetDouble(), "Index 1 should be replaced");
        Assert.AreEqual(30, items[2].GetDouble(), "Index 2 should be added from override");
        Assert.AreEqual(40, items[3].GetDouble(), "Index 3 should be added from override");
    }

    [TestMethod]
    public void MergeInPlaceArrayIndexBasedModeBaseLonger()
    {
        var baseJson = """[1, 2, 3, 4]""";
        var overrideJson = """[10, 20]""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = baseValue.GetArrayValue();

        Assert.AreEqual(4, items.Count, "Array should have max length of both arrays");
        Assert.AreEqual(10, items[0].GetDouble(), "Index 0 should be replaced");
        Assert.AreEqual(20, items[1].GetDouble(), "Index 1 should be replaced");
        Assert.AreEqual(3, items[2].GetDouble(), "Index 2 should remain from base");
        Assert.AreEqual(4, items[3].GetDouble(), "Index 3 should remain from base");
    }

    [TestMethod]
    public void MergeArrayIndexBasedModeBaseLonger()
    {
        var baseJson = """{"items": [1, 2, 3, 4]}""";
        var overrideJson = """{"items": [10, 20]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = result.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(4, items.Count, "Array should have max length of both arrays");
        Assert.AreEqual(10, items[0].GetDouble(), "Index 0 should be replaced");
        Assert.AreEqual(20, items[1].GetDouble(), "Index 1 should be replaced");
        Assert.AreEqual(3, items[2].GetDouble(), "Index 2 should remain from base");
        Assert.AreEqual(4, items[3].GetDouble(), "Index 3 should remain from base");
    }

    [TestMethod]
    public void MergeInPlaceArrayIndexBasedModeWithObjects()
    {
        var baseJson = """{"items": [{"id": 1, "name": "a"}, {"id": 2, "name": "b"}]}""";
        var overrideJson = """{"items": [{"id": 10}, {"id": 20, "name": "z", "active": true}]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = baseValue.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(2, items.Count, "Array should have 2 items");

        // First object: merges {id: 1, name: "a"} with {id: 10}
        var obj0 = items[0].GetObjectValue();
        Assert.AreEqual(10, obj0["id"].GetDouble(), "First object id should be overridden");
        Assert.AreEqual("a", obj0["name"].GetString(), "First object name should be preserved from base");

        // Second object: merges {id: 2, name: "b"} with {id: 20, name: "z", active: true}
        var obj1 = items[1].GetObjectValue();
        Assert.AreEqual(20, obj1["id"].GetDouble(), "Second object id should be overridden");
        Assert.AreEqual("z", obj1["name"].GetString(), "Second object name should be overridden");
        Assert.IsTrue(obj1["active"].GetBoolean(), "Second object active should be added");
    }

    [TestMethod]
    public void MergeArrayIndexBasedModeWithObjects()
    {
        var baseJson = """{"items": [{"id": 1, "name": "a"}, {"id": 2, "name": "b"}]}""";
        var overrideJson = """{"items": [{"id": 10}, {"id": 20, "name": "z", "active": true}]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = result.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(2, items.Count, "Array should have 2 items");

        // First object: merges {id: 1, name: "a"} with {id: 10}
        var obj0 = items[0].GetObjectValue();
        Assert.AreEqual(10, obj0["id"].GetDouble(), "First object id should be overridden");
        Assert.AreEqual("a", obj0["name"].GetString(), "First object name should be preserved from base");

        // Second object: merges {id: 2, name: "b"} with {id: 20, name: "z", active: true}
        var obj1 = items[1].GetObjectValue();
        Assert.AreEqual(20, obj1["id"].GetDouble(), "Second object id should be overridden");
        Assert.AreEqual("z", obj1["name"].GetString(), "Second object name should be overridden");
        Assert.IsTrue(obj1["active"].GetBoolean(), "Second object active should be added");
    }

    [TestMethod]
    public void MergeArrayIndexBasedModeDoesNotModifyOriginal()
    {
        var baseJson = """{"items": [1, 2, 3]}""";
        var overrideJson = """{"items": [10, 20]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = result.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(3, items.Count, "Result array should have max length");
        Assert.AreEqual(10, items[0].GetDouble());
        Assert.AreEqual(20, items[1].GetDouble());
        Assert.AreEqual(3, items[2].GetDouble());

        // Verify original is unchanged
        var baseItems = baseValue.GetObjectValue()["items"].GetArrayValue();
        Assert.AreEqual(3, baseItems.Count, "Base array should remain unchanged");
        Assert.AreEqual(1, baseItems[0].GetDouble(), "Base array values should remain unchanged");
        Assert.AreEqual(2, baseItems[1].GetDouble(), "Base array values should remain unchanged");
        Assert.AreEqual(3, baseItems[2].GetDouble(), "Base array values should remain unchanged");
    }

    [TestMethod]
    public void MergeInPlaceArrayIndexBasedModeWithNestedArrays()
    {
        var baseJson = """{"items": [[1, 2], [3, 4]]}""";
        var overrideJson = """{"items": [[10], [30, 40, 50]]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        JsonCodec.MergeInPlace(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = baseValue.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(2, items.Count, "Outer array should have 2 items");

        // First nested array: merges [1, 2] with [10] using index-based
        var arr0 = items[0].GetArrayValue();
        Assert.AreEqual(2, arr0.Count, "First nested array should have max length of 2");
        Assert.AreEqual(10, arr0[0].GetDouble(), "First nested array index 0 should be replaced");
        Assert.AreEqual(2, arr0[1].GetDouble(), "First nested array index 1 should remain from base");

        // Second nested array: merges [3, 4] with [30, 40, 50] using index-based
        var arr1 = items[1].GetArrayValue();
        Assert.AreEqual(3, arr1.Count, "Second nested array should extend to override length");
        Assert.AreEqual(30, arr1[0].GetDouble(), "Second nested array index 0 should be replaced");
        Assert.AreEqual(40, arr1[1].GetDouble(), "Second nested array index 1 should be replaced");
        Assert.AreEqual(50, arr1[2].GetDouble(), "Second nested array index 2 should be added from override");
    }

    [TestMethod]
    public void MergeArrayIndexBasedModeWithNestedArrays()
    {
        var baseJson = """{"items": [[1, 2], [3, 4]]}""";
        var overrideJson = """{"items": [[10], [30, 40, 50]]}""";

        var baseValue = JsonCodec.Decode(baseJson);
        var overrideValue = JsonCodec.Decode(overrideJson);

        var result = JsonCodec.Merge(baseValue, overrideValue, JsonCodec.ArrayMergeMode.IndexBased);
        var items = result.GetObjectValue()["items"].GetArrayValue();

        Assert.AreEqual(2, items.Count, "Outer array should have 2 items");

        // First nested array: merges [1, 2] with [10] using index-based
        var arr0 = items[0].GetArrayValue();
        Assert.AreEqual(2, arr0.Count, "First nested array should have max length of 2");
        Assert.AreEqual(10, arr0[0].GetDouble(), "First nested array index 0 should be replaced");
        Assert.AreEqual(2, arr0[1].GetDouble(), "First nested array index 1 should remain from base");

        // Second nested array: merges [3, 4] with [30, 40, 50] using index-based
        var arr1 = items[1].GetArrayValue();
        Assert.AreEqual(3, arr1.Count, "Second nested array should extend to override length");
        Assert.AreEqual(30, arr1[0].GetDouble(), "Second nested array index 0 should be replaced");
        Assert.AreEqual(40, arr1[1].GetDouble(), "Second nested array index 1 should be replaced");
        Assert.AreEqual(50, arr1[2].GetDouble(), "Second nested array index 2 should be added from override");
    }
}
