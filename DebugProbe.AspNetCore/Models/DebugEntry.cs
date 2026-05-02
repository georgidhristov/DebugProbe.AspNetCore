namespace DebugProbe.AspNetCore.Models;

public class DebugEntry
{
    public string Id { get; set; } = default!;

    // Request
    public string Path { get; set; } = default!;
    public string Method { get; set; } = default!;
    public string? Query { get; set; }
    public string? RequestUrl { get; set; }
    public string RequestBody { get; set; } = default!;

    // Response
    public int StatusCode { get; set; }
    public string ResponseBody { get; set; } = default!;

    // Metrics
    public long DurationMs { get; set; }         
    public DateTime ServerTimeUtc { get; set; }   

    // Context
    public string Environment { get; set; } = default!;
    public string Culture { get; set; } = default!;

    // Headers
    public Dictionary<string, string> Headers { get; set; } = new();

    public DateTime Timestamp { get; set; }       
}