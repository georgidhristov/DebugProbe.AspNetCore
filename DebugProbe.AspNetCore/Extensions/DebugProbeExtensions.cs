using System.Net.Http.Json;
using DebugProbe.AspNetCore.Internal;
using DebugProbe.AspNetCore.Middleware;
using DebugProbe.AspNetCore.Models;
using DebugProbe.AspNetCore.Options;
using DebugProbe.AspNetCore.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DebugProbe.AspNetCore.Extensions;

public static class DebugProbeExtensions
{
    private static readonly HttpClient Http = new();

    public static IServiceCollection AddDebugProbe(
        this IServiceCollection services,
        Action<DebugProbeOptions>? configure = null)
    {
        var options = new DebugProbeOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<DebugEntryStore>();

        return services;
    }

    public static IApplicationBuilder UseDebugProbe(this IApplicationBuilder app)
    {
        app.UseMiddleware<DebugProbeMiddleware>();

        if (app is WebApplication webApp)
        {
            webApp.MapGet("/debug", async (HttpContext ctx, DebugEntryStore store) =>
            {
                var items = store.GetAll()
                    .OrderByDescending(x => x.Timestamp)
                    .ToList();

                var html = HtmlRenderer.RenderIndexPage(items);

                ctx.Response.ContentType = "text/html";
                await ctx.Response.WriteAsync(html);
            }).ExcludeFromDescription();

            webApp.MapGet("/debug/{id}", async (HttpContext ctx, string id, DebugEntryStore store) =>
            {
                var item = store.Get(id);

                if (item is null)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync("Not found");
                    return;
                }

                var prettyRequest = JsonUtils.Format(item.RequestBody);
                var prettyResponse = JsonUtils.Format(item.ResponseBody);

                var html = HtmlRenderer.RenderDetailsPage(item, prettyRequest, prettyResponse);

                ctx.Response.ContentType = "text/html";
                await ctx.Response.WriteAsync(html);
            }).ExcludeFromDescription();

            webApp.MapGet("/debug/compare/{id}", async (string id, string url, DebugEntryStore store) =>
            {
                var local = store.Get(id);
                if (local is null)
                {
                    return Results.NotFound("Local trace not found");
                }

                DebugEntry? remote;

                try
                {
                    remote = await Http.GetFromJsonAsync<DebugEntry>(url);
                }
                catch
                {
                    return Results.BadRequest("Failed to reach remote server");
                }

                if (remote is null)
                {
                    return Results.NotFound("Remote trace not found");
                }

                var diff = DebugEntryComparer.Compare(local, remote);

                return Results.Ok(new
                {
                    method = new { local = local.Method, remote = remote.Method },
                    path = new { local = local.Path, remote = remote.Path },
                    status = new { local = local.StatusCode, remote = remote.StatusCode },

                    requestTime = new
                    {
                        local = local.RequestTimeUtc.ToLocalTime().ToString("HH:mm:ss"),
                        remote = remote.RequestTimeUtc.ToLocalTime().ToString("HH:mm:ss"),
                    },

                    environment = new { local = local.Environment, remote = remote.Environment },
                    culture = new { local = local.Culture, remote = remote.Culture },
                    requestBody = new { local = local.RequestBody ?? "", remote = remote.RequestBody ?? "" },
                    responseBody = new { local = local.ResponseBody ?? "", remote = remote.ResponseBody ?? "" },

                    diffs = diff
                });
            }).ExcludeFromDescription();

            webApp.MapGet("/debug/json/{id}", (string id, DebugEntryStore store) =>
            {
                var item = store.Get(id);
                return item is null ? Results.NotFound() : Results.Json(item);
            }).ExcludeFromDescription();

            webApp.MapGet("/debug/compare.js", () =>
                Results.Text(EmbeddedResources.CompareJs, "application/javascript")
            ).ExcludeFromDescription();

            webApp.MapGet("/debug/ui.js", () =>
                Results.Text(EmbeddedResources.UiJs, "application/javascript")
            ).ExcludeFromDescription();

            webApp.MapPost("/debug/clear", (DebugEntryStore store) =>
            {
                store.Clear();
                return Results.Ok();
            }).ExcludeFromDescription();

            webApp.Map("/debug/logo.png", ctx =>
                WriteEmbeddedAsset(ctx, "DebugProbe.AspNetCore.Assets.logo.PNG", "image/png")
            ).ExcludeFromDescription();

            webApp.Map("/debug/favicon.ico", ctx =>
                WriteEmbeddedAsset(ctx, "DebugProbe.AspNetCore.Assets.favicon.ico", "image/x-icon")
            ).ExcludeFromDescription();
        }

        return app;
    }

    private static async Task WriteEmbeddedAsset(HttpContext ctx, string resourceName, string contentType)
    {
        ctx.Response.ContentType = contentType;

        var assembly = typeof(DebugProbeMiddleware).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            ctx.Response.StatusCode = 404;
            return;
        }

        await stream.CopyToAsync(ctx.Response.Body);
    }
}
