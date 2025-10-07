// <copyright file="PerformanceTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonPlus.UnitTests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PerformanceTests
{
    [TestMethod]
    public void DecodePerformanceIsWithinAcceptableRange()
    {
        var baselineTime = GetBaselinePerformance();
        var jsonPlusTime = GetJsonPlusPerformance();
        var jsonPlusMemorySpeed = jsonPlusTime / (double)baselineTime;

        Console.WriteLine($"Baseline: {baselineTime}ms, JsonPlus: {jsonPlusTime}ms  ({jsonPlusMemorySpeed:0.00}x times slower)");

#if DEBUG
        var factor = 10;
#else
        var factor = 3;
#endif

        // Check that JsonPlus is within the performance factor of the baseline
        Assert.IsTrue(jsonPlusTime <= baselineTime * factor, $"JsonPlus parsing took {jsonPlusTime}ms, which is more than {factor}x the baseline of {baselineTime}ms.");
    }

    private static int GetJsonPlusPerformance()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var count = 0;
        for (int i = 0; i < 1000; i++)
        {
            foreach (var json in RoundTripTests.TestValues)
            {
                var value = JsonCodec.Decode(json);
                if (value.Kind == JsonValueKind.JsonString)
                {
                    count += value.GetString()!.Length;
                }
            }
        }
        sw.Stop();
        Console.WriteLine($"Processed {count} characters in strings."); // this is just to avoid optimizations removing the loop
        return (int)sw.ElapsedMilliseconds;
    }

    private static int GetBaselinePerformance()
    {
        // use System.Text.Json as baseline
        var options = new System.Text.Json.JsonDocumentOptions
        {
            CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var count = 0;
        for (int i = 0; i < 1000; i++)
        {
            foreach (var json in RoundTripTests.TestValues)
            {
                var doc = System.Text.Json.JsonDocument.Parse(json, options);
                if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    count += doc.RootElement.GetString()!.Length;
                }
            }
        }
        sw.Stop();
        Console.WriteLine($"Processed {count} characters in strings."); // this is just to avoid optimizations removing the loop
        return (int)sw.ElapsedMilliseconds;
    }
}
