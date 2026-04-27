using System.Globalization;
using DebugProbe.AspNetCore.Internal;
using DebugProbe.AspNetCore.Models;
using DebugProbe.AspNetCore.Storage;
using Microsoft.AspNetCore.Http;

namespace DebugProbe.AspNetCore.Middleware;

/// <summary>
/// Middleware that captures HTTP request and response data and stores it
/// as DebugEntry for inspection via the DebugProbe UI.
/// </summary>
public class DebugProbeMiddleware
{
    private readonly RequestDelegate _next;

    public DebugProbeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, DebugEntryStore store)
    {
        /// Skips DebugProbe endpoints to avoid self-tracking.
        if (context.Request.Path.StartsWithSegments("/debug") ||
            context.Request.Path.StartsWithSegments("/debugprobe") ||
            context.Request.Path.StartsWithSegments("/favicon.ico"))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        var originalBody = context.Response.Body;
        using var ms = new MemoryStream();
        context.Response.Body = ms;

        await _next(context);

        ms.Position = 0;
        var responseBody = await new StreamReader(ms).ReadToEndAsync();
        ms.Position = 0;
        await ms.CopyToAsync(originalBody);

        store.Add(new DebugEntry
        {
            Id = Guid.NewGuid().ToString(),

            Path = context.Request.Path,
            Method = context.Request.Method,
            StatusCode = context.Response.StatusCode,
            Query = context.Request.QueryString.ToString(),

            RequestUrl = $"{context.Request.Scheme}://{context.Request.Host}" + 
                    $"{context.Request.Path}{context.Request.QueryString}",

            RequestBody = Trim(requestBody),
            ResponseBody = Trim(responseBody),

            Headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Timestamp = DateTime.UtcNow,

            Environment = EnvironmentUtils.TryGetEnvironment(),
            Culture = CultureInfo.CurrentCulture.Name
        });
    }

    private string Trim(string value, int max = 2000)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= max ? value : value.Substring(0, max);
    }
}