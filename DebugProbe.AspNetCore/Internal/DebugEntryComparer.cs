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

        // --- Basic differences ---
        if (a.StatusCode != b.StatusCode)
            diffs.Add(new { field = "Status", local = a.StatusCode, remote = b.StatusCode, type = "meta" });

        if (a.Environment != b.Environment)
            diffs.Add(new { field = "Environment", local = a.Environment, remote = b.Environment, type = "meta" });

        if (a.Culture != b.Culture)
            diffs.Add(new { field = "Culture", local = a.Culture, remote = b.Culture, type = "meta" });

        // --- Request JSON diff ---
        if (!string.IsNullOrWhiteSpace(a.RequestBody) || !string.IsNullOrWhiteSpace(b.RequestBody))
        {
            try
            {
                using var localJson = JsonDocument.Parse(a.RequestBody ?? "{}");
                using var remoteJson = JsonDocument.Parse(b.RequestBody ?? "{}");

                CompareJson(localJson.RootElement, remoteJson.RootElement, "RequestBody", diffs);
            }
            catch
            {
                if (a.RequestBody != b.RequestBody)
                {
                    diffs.Add(new
                    {
                        field = "RequestBody",
                        local = a.RequestBody,
                        remote = b.RequestBody,
                        type = "request"
                    });
                }
            }
        }

        // --- Response JSON diff ---
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
                    remote = b.ResponseBody,
                    type = "response"
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
                field = Clean(path),
                local = a.ToString(),
                remote = b.ToString(),
                type = GetType(path)
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
                var lenA = a.GetArrayLength();
                var lenB = b.GetArrayLength();
                var len = Math.Min(lenA, lenB);

                for (int i = 0; i < len; i++)
                {
                    CompareJson(a[i], b[i], $"{path}[{i}]", diffs);
                }

                if (lenA != lenB)
                {
                    diffs.Add(new
                    {
                        field = Clean(path),
                        local = $"Length: {lenA}",
                        remote = $"Length: {lenB}",
                        type = GetType(path)
                    });
                }
                break;

            default:
                var aVal = a.ToString();
                var bVal = b.ToString();

                if (aVal != bVal)
                {
                    diffs.Add(new
                    {
                        field = Clean(path),
                        local = aVal,
                        remote = bVal,
                        type = GetType(path)
                    });
                }
                break;
        }
    }

    private static string GetType(string path)
    {
        return path.StartsWith("RequestBody") ? "request" : "response";
    }

    private static string Clean(string path)
    {
        if (path.StartsWith("ResponseBody."))
            return path.Substring("ResponseBody.".Length);

        if (path == "ResponseBody")
            return "";

        if (path.StartsWith("RequestBody."))
            return path.Substring("RequestBody.".Length);

        if (path == "RequestBody")
            return "";

        return path;
    }
}