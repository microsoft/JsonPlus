// <copyright file="JsonCodec.Encode.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus;

using System.Text;

public partial class JsonCodec
{
    public static string Encode(JsonValue value)
    {
        var sb = new StringBuilder();
        AppendValue(value, sb);
        return sb.ToString();
    }

    public static string EncodeString(string value)
    {
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\\u{(int)c:X4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }

    private static void AppendValue(JsonValue value, StringBuilder sb)
    {
        AppendTrivia(value.LeadingTrivia, sb);
        switch (value.Kind)
        {
            case JsonValueKind.JsonNull:
                sb.Append("null");
                break;
            case JsonValueKind.JsonBoolean:
                sb.Append(value.GetBoolean() ? "true" : "false");
                break;
            case JsonValueKind.JsonNumber:
                sb.Append(value.GetRawValue());
                break;
            case JsonValueKind.JsonString:
                sb.Append(value.GetRawValue());
                break;
            case JsonValueKind.JsonArray:
                AppendArray(value.GetArrayValue(), sb);
                break;
            case JsonValueKind.JsonObject:
                AppendObject(value.GetObjectValue(), sb);
                break;
        }
        AppendTrivia(value.TrailingTrivia, sb);
    }

    private static void AppendTrivia(JsonTriviaCollection trivia, StringBuilder sb)
    {
        foreach (var t in trivia)
        {
            sb.Append(t.Value);
        }
    }

    private static void AppendArray(JsonArray array, StringBuilder sb)
    {
        for (int i = 0; i < array.Count; i++)
        {
            AppendValue(array[i], sb);
        }
    }

    private static void AppendObject(JsonObject obj, StringBuilder sb)
    {
        for (int i = 0; i < obj.Count; i++)
        {
            var prop = obj[i];
            AppendTrivia(prop.Key.LeadingTrivia, sb);
            sb.Append(prop.Key.RawValue);
            AppendTrivia(prop.Key.TrailingTrivia, sb);
            AppendValue(prop.Value, sb);
        }
    }
}

