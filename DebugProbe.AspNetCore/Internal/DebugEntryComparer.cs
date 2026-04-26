using System.Text.Json;
using DebugProbe.AspNetCore.Models;

namespace DebugProbe.AspNetCore.Internal;

/// <summary>
/// Compares two DebugEntry instances and produces a list of differences,
/// including JSON-aware comparison of response bodies.
/// </summary>
internal static class DebugEntryComparer
{
    public static List<object> Compare(DebugEntry a, DebugEntry b)
    {
        var diffs = new List<object>();

        if (a.StatusCode != b.StatusCode)
            diffs.Add(new { field = "StatusCode", local = a.StatusCode, remote = b.StatusCode });

        if (a.Environment != b.Environment)
            diffs.Add(new { field = "Environment", local = a.Environment, remote = b.Environment });

        if (a.Culture != b.Culture)
            diffs.Add(new { field = "Culture", local = a.Culture, remote = b.Culture });

        try
        {
            using var localJson = JsonDocument.Parse(a.ResponseBody);
            using var remoteJson = JsonDocument.Parse(b.ResponseBody);

            CompareJson(localJson.RootElement, remoteJson.RootElement, "ResponseBody", diffs);
        }
        catch
        {
            if (a.ResponseBody != b.ResponseBody)
            {
                diffs.Add(new
                {
                    field = "ResponseBody",
                    local = a.ResponseBody,
                    remote = b.ResponseBody
                });
            }
        }

        return diffs;
    }

    private static void CompareJson(JsonElement a, JsonElement b, string path, List<object> diffs)
    {
        if (a.ValueKind != b.ValueKind)
        {
            diffs.Add(new
            {
                field = path.Replace("ResponseBody", ""),
                local = a.ToString(),
                remote = b.ToString()
            });
            return;
        }

        switch (a.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in a.EnumerateObject())
                {
                    if (b.TryGetProperty(prop.Name, out var bProp))
                    {
                        CompareJson(prop.Value, bProp, $"{path}.{prop.Name}", diffs);
                    }
                }
                break;

            case JsonValueKind.Array:
                var len = Math.Min(a.GetArrayLength(), b.GetArrayLength());
                for (int i = 0; i < len; i++)
                {
                    CompareJson(a[i], b[i], $"{path}[{i}]", diffs);
                }
                break;

            default:
                var aVal = a.ToString();
                var bVal = b.ToString();

                diffs.Add(new
                {
                    field = path.Replace("ResponseBody", ""),
                    local = aVal,
                    remote = bVal,
                    isDiff = aVal != bVal
                });
                break;
        }
    }
}

