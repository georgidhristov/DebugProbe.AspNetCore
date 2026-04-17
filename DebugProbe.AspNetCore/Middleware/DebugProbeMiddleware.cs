using System.Globalization;
using DebugProbe.AspNetCore.Models;
using DebugProbe.AspNetCore.Store;
using Microsoft.AspNetCore.Http;


namespace DebugProbe.AspNetCore.Middleware;

public class DebugProbeMiddleware
{
    private readonly RequestDelegate _next;

    public DebugProbeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, RequestStore store)
    {
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
            Path = context.Request.Path,
            Method = context.Request.Method,
            StatusCode = context.Response.StatusCode,
            RequestBody = requestBody,
            ResponseBody = responseBody,
            Headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Culture = CultureInfo.CurrentCulture.Name
        });
    }
}