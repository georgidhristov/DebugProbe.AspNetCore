using System.Text.Json;

namespace DebugProbe.AspNetCore.Internal;

internal static class JsonUtils
{
    public static string Format(string json)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<object>(json);
            return JsonSerializer.Serialize(parsed, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json;
        }
    }
}
