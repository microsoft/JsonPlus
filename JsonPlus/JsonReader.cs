// <copyright file="JsonReader.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus;

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "StringReader wraps a string and doesn't hold unmanaged resources; disposal is not critical")]
public class JsonReader
{
    private static readonly StringReader EmptyStringReader = new(string.Empty);
    private static readonly string[] WhitespacesTokensPool = [.. Enumerable.Range(1, 32).Select(i => new string(' ', i))];
    private static readonly string[] TabsTokensPool = [.. Enumerable.Range(1, 8).Select(i => new string('\t', i))];

    private StringReader input;
    private JsonDecodingOptions decodingOptions;
    private readonly StringBuilder token;
    private readonly StringBuilder stringValue;

    public int Depth { get; private set; }

    public int Index { get; private set; }

    public int Line { get; private set; }

    public int Column { get; private set; }

    public int Current { get; private set; }

    public char CurrentChar => (char)Current;

    public JsonReader()
    {
        input = EmptyStringReader;
        token = new(128);
        stringValue = new(128);
    }

    public void Init(string input, JsonDecodingOptions options)
    {
        this.input = new StringReader(input);
        decodingOptions = options;
        Depth = 0;
        Index = 0;
        Line = 1;
        Column = 1;
        Current = this.input.Read();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read()
    {
        token.Append((char)Current);
        Current = input.Read();
        Index++;
        Column++;
    }

    public JsonParsingException CreateParsingException(string message)
    {
        if (Current == -1)
        {
            token.Append("<EOF>");
        }
        else if (char.IsControl(CurrentChar))
        {
            token.Append(System.Globalization.CultureInfo.InvariantCulture, $"\\u{Current:X4}");
        }
        else
        {
            token.Append(CurrentChar);
        }
        return new JsonParsingException(message, token.ToString(), new JsonPosition(Index, Line, Column, Depth));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartObject()
    {
        Depth++;
        if (Depth > decodingOptions.MaxNestingDepth)
        {
            throw CreateParsingException($"Maximum allowed nesting depth of exceeded");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartArray()
    {
        Depth++;
        if (Depth > decodingOptions.MaxNestingDepth)
        {
            throw CreateParsingException($"Maximum allowed nesting depth of exceeded");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndObject()
    {
        if (Depth == 0)
        {
            throw CreateParsingException("Unexpected closing '}'");
        }
        Depth--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndArray()
    {
        if (Depth == 0)
        {
            throw CreateParsingException("Unexpected closing ']'");
        }
        Depth--;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadLiteralToken(string expected)
    {
        Current = input.Read(); // consume the first character
        Index++;
        Column++;
        for (var i = 1; i < expected.Length; i++)
        {
            if (Current != expected[i])
            {
                throw CreateParsingException($"Expected literal '{expected}'");
            }
            Current = input.Read();
            Index++;
            Column++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JsonTrivia ReadNewLineToken()
    {
        Current = input.Read(); // consume the newline character
        Index++;
        Line++;
        Column = 1;
        return new JsonTrivia(JsonTriviaKind.NewLine, "\n");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JsonTrivia ReadCarriageReturnToken()
    {
        Current = input.Read(); // consume the newline character
        Index++;
        Line++;
        Column = 1;
        if (Current == '\n')
        {
            Current = input.Read(); // consume the newline character
            Index++;
            return new JsonTrivia(JsonTriviaKind.NewLine, "\r\n");
        }
        return new JsonTrivia(JsonTriviaKind.NewLine, "\r");
    }

    public JsonTrivia ReadTabsToken()
    {
        var startIndex = Index;
        Current = input.Read(); // consume first tab character
        Index++;
        Column++;
        while (Current == '\t')
        {
            Current = input.Read();
            Index++;
            Column++;
        }
        var len = Index - startIndex;
        var token = len <= TabsTokensPool.Length ? TabsTokensPool[len - 1] : new string('\t', len);
        return new JsonTrivia(JsonTriviaKind.Whitespace, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public JsonTrivia ReadWhitespacesToken()
    {
        var startIndex = Index;
        Current = input.Read(); // consume first whitespace character
        Index++;
        Column++;
        while (Current == ' ')
        {
            Current = input.Read();
            Index++;
            Column++;
        }
        var len = Index - startIndex;
        var token = len <= WhitespacesTokensPool.Length ? WhitespacesTokensPool[len - 1] : new string(' ', len);
        return new JsonTrivia(JsonTriviaKind.Whitespace, token);
    }

    public JsonTrivia ReadCommentToken()
    {
        StartReadToken();
        Read(); // consume the initial '/'
        if (Current == '/')
        {
            if (!decodingOptions.AllowSingleLineComments)
            {
                throw CreateParsingException("Single-line comments are not allowed");
            }
            Read(); // consume the second '/'
            while (Current != '\n' && Current != '\r' && Current != -1)
            {
                Read();
            }
            return new JsonTrivia(JsonTriviaKind.SingleLineComment, GetToken());
        }
        else if (Current == '*')
        {
            if (!decodingOptions.AllowMultiLineComments)
            {
                throw CreateParsingException("Multi-line comments are not allowed");
            }
            Read(); // consume the '*'
            while (true)
            {
                if (Current == '*')
                {
                    Read(); // consume the '*'
                    if (Current == '/')
                    {
                        Read(); // consume the '/'
                        break;
                    }
                }
                else if (Current == -1)
                {
                    throw CreateParsingException("Unterminated multi-line comment");
                }
                else if (Current == '\n')
                {
                    Read(); // consume the newline character
                    Line++;
                    Column = 1;
                }
                else if (Current == '\r')
                {
                    Read(); // consume the carriage return character
                    if (Current == '\n')
                    {
                        Read();
                    }
                    Line++;
                    Column = 1;
                }
                else
                {
                    Read();
                }
            }
            return new JsonTrivia(JsonTriviaKind.MultiLineComment, GetToken());
        }
        else
        {
            throw CreateParsingException("Invalid comment start - expected '/' or '*' after '/'");
        }
    }

    public string ReadNumberToken()
    {
        StartReadToken();
        if (Current == '-') // Optional leading minus
        {
            Read();
        }
        if (Current == '0') // Read integer part
        {
            Read();
        }
        else if (IsDigit(Current))
        {
            while (IsDigit(Current))
            {
                Read();
            }
        }
        else
        {
            throw CreateParsingException("Invalid number format");
        }
        if (Current == '.') // Optional decimal part
        {
            Read();
            if (!IsDigit(Current))
            {
                throw CreateParsingException("Expected digit after decimal point");
            }
            while (IsDigit(Current))
            {
                Read();
            }
        }
        if (Current == 'e' || Current == 'E') // Optional exponent part
        {
            Read();
            if (Current == '+' || Current == '-')
            {
                Read();
            }
            if (!IsDigit(Current))
            {
                throw CreateParsingException("Expected digit in exponent");
            }
            while (IsDigit(Current))
            {
                Read();
            }
        }
        return GetToken();
    }

    public void ReadStringToken(out string strValue, out string rawValue)
    {
        stringValue.Clear();
        StartReadToken();
        Read(); // consume opening quote
        while (true)
        {
            if (Current == '"')
            {
                break;
            }
            else if (Current == -1)
            {
                throw CreateParsingException("Unterminated string literal");
            }
            else if (Current < 0x20)
            {
                throw CreateParsingException($"Invalid character with code: `{Current}` in string literal");
            }
            else if (Current == '\\')
            {
                Read(); // consume the backslash
                switch (Current)
                {
                    case '"': Read(); stringValue.Append('"'); break;
                    case '/': Read(); stringValue.Append('/'); break;
                    case 'b': Read(); stringValue.Append('\b'); break;
                    case 'f': Read(); stringValue.Append('\f'); break;
                    case 'n': Read(); stringValue.Append('\n'); break;
                    case 'r': Read(); stringValue.Append('\r'); break;
                    case 't': Read(); stringValue.Append('\t'); break;
                    case '\\': Read(); stringValue.Append('\\'); break;
                    case 'u': stringValue.Append(ReadUnicodeEscape()); break;
                    case -1: throw CreateParsingException("Unterminated escape sequence in string literal");
                    default: throw CreateParsingException($"Invalid escape character '\\{CurrentChar}' in string literal");
                }
            }
            else
            {
                stringValue.Append(CurrentChar);
                Read(); // consume regular character
            }
        }
        Read(); // consume closing quote
        rawValue = GetToken();
        strValue = stringValue.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(int c)
    {
        return c >= '0' && c <= '9';
    }

    private string ReadUnicodeEscape()
    {
        Read(); // consume the 'u'
        var ch = 0;
        for (var i = 0; i < 4; i++)
        {
            ch <<= 4;
            ch += Current switch
            {
                >= '0' and <= '9' => Current - '0',
                >= 'a' and <= 'f' => Current - 'a' + 10,
                >= 'A' and <= 'F' => Current - 'A' + 10,
                _ => throw CreateParsingException("Invalid hex character in escape sequence"),
            };
            Read(); // consume each hex digit
        }
        return char.ConvertFromUtf32(ch);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetToken()
    {
        return token.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StartReadToken()
    {
        token.Clear();
    }
}

