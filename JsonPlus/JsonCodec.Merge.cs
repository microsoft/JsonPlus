// <copyright file="JsonCodec.Merge.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus;

using System;
using System.Linq;

public partial class JsonCodec
{
    /// <summary>
    /// Defines how arrays should be merged.
    /// </summary>
    public enum ArrayMergeMode
    {
        /// <summary>
        /// Merge the individual items based on their indices.
        /// </summary>
        IndexBased,

        /// <summary>
        /// Replace the base array with the override array.
        /// </summary>
        Replace,

        /// <summary>
        /// Union the values from both arrays.
        /// </summary>
        Union,
    }

    /// <summary>
    /// Merges two JSON values recursively, modifying the base value in place.
    /// For objects, properties from override are added to base or merged recursively.
    /// For arrays, behavior depends on the merge mode.
    /// For primitives, the override value replaces the base value.
    /// </summary>
    /// <param name="baseValue">The base JSON value to merge into (modified in place).</param>
    /// <param name="overrideValue">The override JSON value to merge from.</param>
    /// <param name="arrayMergeMode">Specifies how arrays should be merged.</param>
    /// <returns>The merged base value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when baseValue or overrideValue is null.</exception>
    public static void MergeInPlace(JsonValue baseValue, JsonValue overrideValue, ArrayMergeMode arrayMergeMode = ArrayMergeMode.Replace)
    {
        ArgumentNullException.ThrowIfNull(baseValue);
        ArgumentNullException.ThrowIfNull(overrideValue);
        if (baseValue.IsPrimitive)
        {
            throw new InvalidOperationException("Cannot perform in-place merge on primitive JsonValue.");
        }
        Merge(baseValue, baseValue, overrideValue, arrayMergeMode);
    }

    /// <summary>
    /// Merges two JSON values recursively, creating a new JSON value without modifying the originals.
    /// For objects, properties from override are added to base or merged recursively.
    /// For arrays, behavior depends on the merge mode.
    /// For primitives, the override value replaces the base value.
    /// </summary>
    /// <param name="baseValue">The base JSON value to merge from.</param>
    /// <param name="overrideValue">The override JSON value to merge from.</param>
    /// <param name="arrayMergeMode">Specifies how arrays should be merged.</param>
    public static JsonValue Merge(JsonValue baseValue, JsonValue overrideValue, ArrayMergeMode arrayMergeMode = ArrayMergeMode.Replace)
    {
        ArgumentNullException.ThrowIfNull(baseValue);
        ArgumentNullException.ThrowIfNull(overrideValue);
        return Merge(null, baseValue, overrideValue, arrayMergeMode);
    }

    private static JsonValue Merge(JsonValue? resultValue, JsonValue baseValue, JsonValue overrideValue, ArrayMergeMode arrayMergeMode)
    {
        // If types don't match or override is a primitive, replace with override
        if (baseValue.Kind != overrideValue.Kind || overrideValue.IsPrimitive)
        {
            // For in-place merge, we need to replace the content but can't change primitive references
            // So we just return the clone - the caller needs to handle updating references
            return CloneValue(overrideValue);
        }

        if (baseValue.Kind == JsonValueKind.JsonObject && overrideValue.Kind == JsonValueKind.JsonObject)
        {
            return MergeObjects(resultValue?.GetObjectValue() ?? [], baseValue.GetObjectValue(), overrideValue.GetObjectValue(), arrayMergeMode);
        }

        if (baseValue.Kind == JsonValueKind.JsonArray && overrideValue.Kind == JsonValueKind.JsonArray)
        {
            return MergeArrays(resultValue?.GetArrayValue() ?? [], baseValue.GetArrayValue(), overrideValue.GetArrayValue(), arrayMergeMode);
        }

        throw new InvalidOperationException("Merge called with incompatible JsonValue kinds.");
    }

    private static JsonObject MergeObjects(JsonObject resultObj, JsonObject baseObj, JsonObject overrideObj, ArrayMergeMode arrayMergeMode)
    {
        var inPlaceMerging = ReferenceEquals(resultObj, baseObj);
        foreach (var baseProp in baseObj.GetProperties())
        {
            var key = baseProp.Key.Value;
            if (!overrideObj.ContainsKey(key))
            {
                // property exists only in base - copy it (only needed if not merging in-place)
                if (!inPlaceMerging)
                {
                    resultObj.AddProperty(key, CloneValue(baseProp.Value));
                }
            }
            else
            {
                var overridePropValue = overrideObj.GetPropertyValue(key);
                resultObj.TryGetValue(key, out var resultPropValue);
                resultObj[key] = Merge(resultPropValue, baseProp.Value, overridePropValue, arrayMergeMode);
            }
        }

        // add properties that exist only in override
        foreach (var overrideProp in overrideObj.GetProperties())
        {
            var key = overrideProp.Key.Value;
            if (!baseObj.ContainsKey(key))
            {
                resultObj.AddProperty(key, CloneValue(overrideProp.Value));
            }
        }

        return resultObj;
    }

    private static JsonArray MergeArrays(JsonArray resultArray, JsonArray baseArray, JsonArray overrideArray, ArrayMergeMode arrayMergeMode)
    {
        return arrayMergeMode switch
        {
            ArrayMergeMode.IndexBased => IndexBasedMergeArrays(resultArray, baseArray, overrideArray, arrayMergeMode),
            ArrayMergeMode.Replace => ReplaceMergeArrays(resultArray, baseArray, overrideArray, arrayMergeMode),
            ArrayMergeMode.Union => UnionMergeArrays(resultArray, baseArray, overrideArray, arrayMergeMode),
            _ => throw new InvalidOperationException($"Unknown ArrayMergeMode: {arrayMergeMode}"),
        };
    }

    private static JsonArray IndexBasedMergeArrays(JsonArray resultArray, JsonArray baseArray, JsonArray overrideArray, ArrayMergeMode arrayMergeMode)
    {
        var inPlaceMerging = ReferenceEquals(resultArray, baseArray);
        var maxLength = Math.Max(baseArray.Count, overrideArray.Count);
        for (var i = 0; i < maxLength; i++)
        {
            var mergedItem = JsonNull.Instance as JsonValue;
            if (i < baseArray.Count && i < overrideArray.Count)
            {
                // both items exist - merge them
                mergedItem = Merge(null, baseArray[i], overrideArray[i], arrayMergeMode);
            }
            else if (i < baseArray.Count)
            {
                // only base item exists
                mergedItem = CloneValue(baseArray[i]);
            }
            else if (i < overrideArray.Count)
            {
                // only override item exists
                mergedItem = CloneValue(overrideArray[i]);
            }
            if (inPlaceMerging && i < resultArray.Count)
            {
                resultArray[i] = mergedItem;
            }
            else
            {
                resultArray.AddParsedValue(mergedItem);
            }
        }
        return resultArray;
    }

    private static JsonArray ReplaceMergeArrays(JsonArray resultArray, JsonArray baseArray, JsonArray overrideArray, ArrayMergeMode arrayMergeMode)
    {
        if (ReferenceEquals(resultArray, baseArray))
        {
            resultArray.Clear(); // clear existing items if merging in-place
        }
        foreach (var item in overrideArray)
        {
            resultArray.AddParsedValue(CloneValue(item));
        }
        return resultArray;
    }

    private static JsonArray UnionMergeArrays(JsonArray resultArray, JsonArray baseArray, JsonArray overrideArray, ArrayMergeMode arrayMergeMode)
    {
        if (!ReferenceEquals(resultArray, baseArray))
        {
            // if not merging in-place, first copy all items from base
            foreach (var item in baseArray)
            {
                resultArray.AddParsedValue(CloneValue(item));
            }
        }
        foreach (var overrideItem in overrideArray)
        {
            // add items from override that are not already in result
            if (!resultArray.Any(resultItem => resultItem.Equals(overrideItem)))
            {
                resultArray.AddParsedValue(CloneValue(overrideItem));
            }
        }
        return resultArray;
    }

    private static JsonValue CloneValue(JsonValue value)
    {
        return value.Kind switch
        {
            JsonValueKind.JsonNull => new JsonNull(),
            JsonValueKind.JsonBoolean => new JsonBoolean(value.GetBoolean()),
            JsonValueKind.JsonNumber => new JsonNumber(value.GetRawValue()),
            JsonValueKind.JsonString => new JsonString(value.GetString(), value.GetRawValue()),
            JsonValueKind.JsonArray => CloneArray(value.GetArrayValue()),
            JsonValueKind.JsonObject => CloneObject(value.GetObjectValue()),
            _ => throw new InvalidOperationException($"Unknown JsonValueKind: {value.Kind}"),
        };
    }

    private static JsonArray CloneArray(JsonArray array)
    {
        var result = new JsonArray();
        foreach (var item in array)
        {
            result.AddParsedValue(CloneValue(item));
        }
        return result;
    }

    private static JsonObject CloneObject(JsonObject obj)
    {
        var result = new JsonObject();
        foreach (var prop in (System.Collections.Generic.IEnumerable<JsonProperty>)obj)
        {
            var clonedKey = new JsonString(prop.Key.Value, prop.Key.RawValue);
            var clonedValue = CloneValue(prop.Value);
            result.AddParsedProperty(new JsonProperty(clonedKey, clonedValue));
        }
        return result;
    }
}
