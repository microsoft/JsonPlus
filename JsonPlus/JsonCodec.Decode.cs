// <copyright file="JsonCodec.Decode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus;

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// Represents the position within a JSON document.
/// </summary>
/// <param name="Index">Index in the input.</param>
/// <param name="Line">Line in the input, starting at 1.</param>
/// <param name="Column">Column in the input, starting at 1.</param>
/// <param name="Depth">Nesting depth of arrays and objects.</param>
public record struct JsonPosition(int Index = 0, int Line = 0, int Column = 0, int Depth = 0);

/// <summary>
/// Exception thrown when a JSON parsing error occurs.
/// </summary>
/// <param name="message">Description of the exception.</param>
/// <param name="token">The currently parsed token.</param>
/// <param name="position">The current position in the input.</param>
[Serializable]
public class JsonParsingException(string message, string token, JsonPosition position) : Exception(message)
{
    /// <summary>
    /// Creates a new instance of the <see cref="JsonParsingException"/> class.
    /// </summary>
    /// <param name="message">Description of the exception.</param>
    /// <param name="token">The currently parsed token.</param>
    public JsonParsingException(string message, string token) : this(message, token, new JsonPosition())
    {
    }

    /// <summary>
    /// The position at which the parsing exception occurred.
    /// </summary>
    public JsonPosition Position { get; } = position;

    /// <inheritdoc />
    public override string ToString() => $"{Message} (found token: `{token}` at line: {Position.Line}, column: {Position.Column}, index: {Position.Index})";
}

/// <summary>
/// Options for decoding JSON input with more relaxed syntax rules.
/// </summary>
/// <param name="AllowTrailingCommas">Allows trailing commas in the input.</param>
/// <param name="AllowSingleLineComments">Allows single line comments in the input.</param>
/// <param name="AllowMultiLineComments">Allows multi line comments in the input.</param>
/// <param name="MaxNestingDepth">Controls the maximum nesting depth at which the decoding will stop.</param>
public record struct JsonDecodingOptions(bool AllowTrailingCommas, bool AllowSingleLineComments, bool AllowMultiLineComments, int MaxNestingDepth);

/// <summary>
/// A Concrete Syntax Tree JSON parser that produces a <see cref="JsonValue" />, including comments and whitespace as trivia.
/// </summary>
public partial class JsonCodec
{
    private static readonly ThreadLocal<JsonCodec> Instance = new(() => new JsonCodec());

    private readonly JsonReader reader = new();

    private JsonTriviaCollection currentTrivia = [];

    private JsonDecodingOptions decodingOptions;

    public static readonly JsonDecodingOptions StrictDecodingOptions = new(AllowMultiLineComments: false, AllowSingleLineComments: false, AllowTrailingCommas: false, MaxNestingDepth: 100);

    public static readonly JsonDecodingOptions RelaxedDecodingOptions = new(AllowMultiLineComments: true, AllowSingleLineComments: true, AllowTrailingCommas: true, MaxNestingDepth: 1000);

    public static JsonValue Decode(string json)
    {
        var codec = Instance.Value ?? throw new InvalidOperationException("Failed to initialize JsonCodec instance");
        return codec.DecodeValue(json, RelaxedDecodingOptions);
    }

    public static JsonValue Decode(string json, JsonDecodingOptions options)
    {
        var codec = Instance.Value ?? throw new InvalidOperationException("Failed to initialize JsonCodec instance");
        return codec.DecodeValue(json, options);
    }

    private JsonValue DecodeValue(string json, JsonDecodingOptions options)
    {
        ArgumentNullException.ThrowIfNull(json);
        decodingOptions = options;
        reader.Init(json, options);
        currentTrivia.Clear();
        ParseWhiteSpaceAndCommentsTrivia(currentTrivia, false);
        var value = ParseValue();
        value.TrailingTrivia.AddRange(currentTrivia);
        CheckEndOfInput();
        return value;
    }

    private void CheckEndOfInput()
    {
        if (reader.Current != -1)
        {
            throw CreateParsingException("Expected end of input");
        }
        if (reader.Depth > 0)
        {
            throw CreateParsingException("Unclosed structure at end of input");
        }
    }

    private JsonParsingException CreateParsingException(string message)
    {
        return reader.CreateParsingException(message);
    }

    private JsonValue ParseValue()
    {
        var value = reader.Current switch
        {
            '[' => ParseArray() as JsonValue,
            '{' => ParseObject(),
            '"' => ParseString(),
            'n' => ParseNull(),
            't' => ParseBoolean(true, "true"),
            'f' => ParseBoolean(false, "false"),
            '-' or (>= '0' and <= '9') => ParseNumber(),
            -1 => throw CreateParsingException("Unexpected end of input"),
            _ => throw CreateParsingException($"Unexpected character '{reader.CurrentChar}'"),
        };
        return value;
    }

    private JsonNull ParseNull()
    {
        reader.ReadLiteralToken("null");
        var value = new JsonNull();
        ParsePrimitiveValueTrivia(value);
        return value;
    }

    private JsonBoolean ParseBoolean(bool expectedValue, string expectedLiteral)
    {
        reader.ReadLiteralToken(expectedLiteral);
        var value = new JsonBoolean(expectedValue);
        ParsePrimitiveValueTrivia(value);
        return value;
    }

    private JsonNumber ParseNumber()
    {
        var rawValue = reader.ReadNumberToken();
        var value = new JsonNumber(rawValue);
        ParsePrimitiveValueTrivia(value);
        return value;
    }

    private JsonString ParseString()
    {
        reader.ReadStringToken(out var strValue, out var rawValue);
        var value = new JsonString(strValue, rawValue);
        ParsePrimitiveValueTrivia(value);
        return value;
    }

    private JsonArray ParseArray()
    {
        var array = new JsonArray();
        reader.StartArray();
        ParseLeadingTrivia(array, JsonTriviaKind.ArrayStart, "[");
        while (true)
        {
            if (reader.Current == ']')
            {
                break;
            }
            var value = ParseValue();
            array.AddParsedValue(value);
            if (reader.Current == ']')
            {
                break;
            }
            if (reader.Current != ',')
            {
                throw CreateParsingException("Expected ',' or ']' in array");
            }
            ParseTrailingTrivia(value, JsonTriviaKind.Comma, ",");
        }
        if (!decodingOptions.AllowTrailingCommas && array.Count > 0)
        {
            if (array[array.Count - 1].TrailingTrivia.Any(t => t.Kind == JsonTriviaKind.Comma))
            {
                throw CreateParsingException("Trailing commas are not allowed in objects");
            }
        }
        ParseTrailingTrivia(array, JsonTriviaKind.ArrayEnd, "]");
        reader.EndArray();
        return array;
    }

    private JsonObject ParseObject()
    {
        var obj = new JsonObject();
        reader.StartObject();
        ParseLeadingTrivia(obj, JsonTriviaKind.ObjectStart, "{");
        while (true)
        {
            if (reader.Current == '}')
            {
                break;
            }
            var key = ParseKey();
            var value = ParseValue();
            var property = new JsonProperty(key, value);
            obj.AddParsedProperty(property);
            if (reader.Current == '}')
            {
                break;
            }
            if (reader.Current != ',')
            {
                throw CreateParsingException("Expected ',' or '}' in object");
            }
            ParseTrailingTrivia(value, JsonTriviaKind.Comma, ",");
        }
        if (!decodingOptions.AllowTrailingCommas && obj.Count > 0)
        {
            if (obj[obj.Count - 1].Value.TrailingTrivia.Any(t => t.Kind == JsonTriviaKind.Comma))
            {
                throw CreateParsingException("Trailing commas are not allowed in objects");
            }
        }
        ParseTrailingTrivia(obj, JsonTriviaKind.ObjectEnd, "}");
        reader.EndObject();
        return obj;
    }

    private JsonString ParseKey()
    {
        if (reader.Current != '"')
        {
            throw CreateParsingException("Expected string as key in object");
        }
        var key = ParseString();
        if (reader.Current != ':')
        {
            throw CreateParsingException("Expected ':' after object key");
        }
        reader.Read(); // consume ':'
        key.TrailingTrivia.Add(new JsonTrivia(JsonTriviaKind.Colon, ":"));
        ParseWhiteSpaceAndCommentsTrivia(currentTrivia, true);
        if (currentTrivia[currentTrivia.Count - 1].Kind == JsonTriviaKind.NewLine)
        {
            // if there is a newline before the value, all trivia belongs to the key's trailing trivia
            key.TrailingTrivia.AddRange(currentTrivia);
            currentTrivia.Clear();
        }
        ParseWhiteSpaceAndCommentsTrivia(currentTrivia, false); // parse trivia after newline
        return key;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ParseWhiteSpaceAndCommentsTrivia(JsonTriviaCollection trivia, bool stopAfterNewLine)
    {
        var stop = false;
        while (!stop)
        {
            switch (reader.Current)
            {
                case '/': trivia.Add(reader.ReadCommentToken()); break;
                case ' ': trivia.Add(reader.ReadWhitespacesToken()); break;
                case '\t': trivia.Add(reader.ReadTabsToken()); break;
                case '\n': trivia.Add(reader.ReadNewLineToken()); stop = stopAfterNewLine; break;
                case '\r': trivia.Add(reader.ReadCarriageReturnToken()); stop = stopAfterNewLine; break;
                default: return;
            }
        }
    }

    private void ParsePrimitiveValueTrivia(JsonValue value)
    {
        var tmp = value.LeadingTrivia;
        value.LeadingTrivia = currentTrivia;
        ParseWhiteSpaceAndCommentsTrivia(value.TrailingTrivia, true);
        currentTrivia = tmp;
        ParseWhiteSpaceAndCommentsTrivia(currentTrivia, false);
    }

    private void ParseLeadingTrivia(JsonValue value, JsonTriviaKind currentTriviaTokenKind, string currentTriviaTokenValue)
    {
        var tmp = value.LeadingTrivia;
        value.LeadingTrivia = currentTrivia;
        value.LeadingTrivia.Add(new JsonTrivia(currentTriviaTokenKind, currentTriviaTokenValue));
        reader.Read(); // consume the current token
        ParseWhiteSpaceAndCommentsTrivia(value.LeadingTrivia, true);
        currentTrivia = tmp;
        ParseWhiteSpaceAndCommentsTrivia(currentTrivia, false);
    }

    private void ParseTrailingTrivia(JsonValue value, JsonTriviaKind currentTriviaTokenKind, string currentTriviaTokenValue)
    {
        var tmp = value.TrailingTrivia;
        if (value.TrailingTrivia.Count == 0)
        {
            value.TrailingTrivia = currentTrivia;
        }
        else
        {
            value.TrailingTrivia.AddRange(currentTrivia);
            tmp = [];
        }
        value.TrailingTrivia.Add(new JsonTrivia(currentTriviaTokenKind, currentTriviaTokenValue));
        reader.Read(); // consume the current token
        ParseWhiteSpaceAndCommentsTrivia(value.TrailingTrivia, true);
        currentTrivia = tmp;
        ParseWhiteSpaceAndCommentsTrivia(currentTrivia, false);
    }
}
