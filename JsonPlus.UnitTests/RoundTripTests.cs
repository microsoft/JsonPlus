// <copyright file="RoundTripTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RoundTripTests
{
    internal static string[] TestValues =
    [
        """{}""",
        """[{}]""",
        """true""",
        """false""",
        """null""",
        """12345""",
        """-123.45e+6""",
        """ "text" """,
        """{"key": "value"}""",
        """{"number": 12345}""",
        """{"boolean": true, "nullValue": null}""",
        """{"emptyArray": [], "emptyObject": {}}""",
        """{"array": [1, 2, 3, 4, 5]}""",
        """{"nested": {"innerKey": "innerValue"}}""",
        """{"mixed": [1, "two", {"three": 3}, [4]]}""",
        """{"escapedString": "Line1\nLine2\tTabbed\"Quote\""}""",
        """{"whitespace": "   \n\t  "}""",
        """{"specialChars": "!@#$%^&*()_+-=[]{}|;:',.<>?/`~"}""",
        """{"unicode": "\u0041\u0042\u0043"}""",
        """{"largeNumber": 12345678901234567890}""",
        """{"floatNumber": 123.456e-7}""",
        """
        {

            "a": /* trivia after key a */ {
            // trivia before key b
                "b": {
                    "c": [1, 2, 3]
                } // end b
                ,
                // test comment
            } , // end a
            // end of object comment
        } // end of root object comment
        """,
        """
        {
            "empty-a": [
            ],
            "empty-o": { /* comment inside object */ },
            "a": [ // Comment before array
                1,
                2,
                3, // Comment inside array
                {
                    "k": "nestedValue"
                },
                [4, 5, 6], [7, 8, 9] /* Another
                comment */
            ], // end array comment
            "o": {
                "key1": "value1",
                "key2": 2,
                "key3": [1, 2, 3,],
            }, // end object comment
        }
        """,
        "\t\t\t\t[\r\n\t\t\t\t1,\r\n\t\t\t\t2,\r\n\t\t\t\t3,\r\n\t\t\t\t4,\r\n\t\t\t\t5,\r\n\t\t\t\t]\t\t\t\t\r\n\r\n\n\n\n\n\r\r\r\r/*end marker*/",
    ];

    [TestMethod]
    public void RoundTripDecodeEncodeReturnsSameJsonIncludingWhiteSpacesAndComments()
    {
        foreach (var json in TestValues)
        {
            TestCodecRoundtrip(json);
        }
    }

    [TestMethod]
    public void RoundTripEncodeDecodeString()
    {
        var c1 = char.ConvertFromUtf32(1); // control character
        var c2 = char.ConvertFromUtf32(0x1F600); // 😀
        var s = $"test\\\b\f\n\r\t\"value\"{c1}{c2}";
        var j = JsonCodec.Encode(new JsonString(s));
        var d = JsonCodec.Decode(j);
        Assert.AreEqual(s, d.GetString());
    }

    [TestMethod]
    public void DecodeReturnsSameValuesAsSystemTextJson()
    {
        var options = new System.Text.Json.JsonDocumentOptions
        {
            CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        foreach (var json in TestValues)
        {
            var expected = System.Text.Json.JsonDocument.Parse(json, options).RootElement;
            var actual = JsonCodec.Decode(json);
            if (!Compare(expected, actual))
            {
                actual = JsonCodec.Decode(json);
                Compare(expected, actual);
                Assert.Fail($"Parsed value do not match with the original json string.\nOriginal: {json}\nParsed:   {JsonCodec.Encode(actual)}");
                Console.WriteLine("Debug representation:");
                Console.WriteLine(JsonCodec.Debug(actual));
                Console.WriteLine();
            }
        }
    }

    static bool Compare(System.Text.Json.JsonElement expected, JsonValue actual)
    {
        return expected.ValueKind switch
        {
            System.Text.Json.JsonValueKind.Object => CompareObjects(expected, actual.GetObjectValue()),
            System.Text.Json.JsonValueKind.Array => CompareArrays(expected, actual.GetArrayValue()),
            System.Text.Json.JsonValueKind.String => expected.GetString() == actual.GetString(),
            System.Text.Json.JsonValueKind.Number => expected.GetDouble() == actual.GetDouble(),
            System.Text.Json.JsonValueKind.True => actual.GetBoolean() == true,
            System.Text.Json.JsonValueKind.False => actual.GetBoolean() == false,
            System.Text.Json.JsonValueKind.Null => actual.Kind == JsonValueKind.JsonNull,
            _ => throw new NotSupportedException($"Unsupported JsonValueKind: {expected.ValueKind}"),
        };
    }

    static bool CompareObjects(System.Text.Json.JsonElement expected, JsonObject actual)
    {
        var expectedCount = expected.EnumerateObject().Count();
        if (expectedCount != actual.Count)
        {
            return false;
        }
        foreach (var prop in expected.EnumerateObject())
        {
            if (!actual.TryGetValue(prop.Name, out var actualValue))
            {
                return false;
            }
            if (!Compare(prop.Value, actualValue))
            {
                return false;
            }
        }
        return true;
    }

    static bool CompareArrays(System.Text.Json.JsonElement expected, JsonArray actual)
    {
        var expectedItems = expected.EnumerateArray().ToList();
        if (expectedItems.Count != actual.Count)
        {
            return false;
        }
        for (var i = 0; i < expectedItems.Count; i++)
        {
            if (!Compare(expectedItems[i], actual[i]))
            {
                return false;
            }
        }
        return true;
    }

    static void TestCodecRoundtrip(string json)
    {
        var val = JsonCodec.Decode(json);
        var str = JsonCodec.Encode(val);
        if (str != json)
        {
            Assert.Fail($"Parsed value do not match with the original json string.\nOriginal:\n{json}\nParsed:\n{str}");
            Console.WriteLine("Debug representation:");
            Console.WriteLine(JsonCodec.Debug(val));
            Console.WriteLine();
        }
    }
}
