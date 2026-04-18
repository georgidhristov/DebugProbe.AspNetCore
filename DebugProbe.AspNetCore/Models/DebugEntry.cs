namespace DebugProbe.AspNetCore.Models;

public class DebugEntry
{
    public string Id { get; set; } = default!;
    public string Path { get; set; } = default!;
    public string Method { get; set; } = default!;
    public int StatusCode { get; set; }
    public string? Query { get; set; }
    public string RequestBody { get; set; } = default!;
    public string ResponseBody { get; set; } = default!;
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; } = default!;
    public string Culture { get; set; } = default!;
}