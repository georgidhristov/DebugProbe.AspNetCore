using System.Net.Http.Json;
using System.Text.Json;
using DebugProbe.AspNetCore.Middleware;
using DebugProbe.AspNetCore.Models;
using DebugProbe.AspNetCore.Options;
using DebugProbe.AspNetCore.Store;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DebugProbe.AspNetCore.Extensions;

public static class DebugProbeExtensions
{
    public static IServiceCollection AddDebugProbe(
        this IServiceCollection services, 
        Action<DebugProbeOptions>? configure = null)
    {
        var options = new DebugProbeOptions();

        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<RequestStore>();

        return services;
    }

    public static IApplicationBuilder UseDebugProbe(this IApplicationBuilder app)
    {
        app.UseMiddleware<DebugProbeMiddleware>();

        if (app is WebApplication webApp)
        {
            webApp.MapGet("/debug", async (HttpContext ctx, RequestStore store) =>
            {
                var items = store.GetAll()
                    .OrderByDescending(x => x.Timestamp)
                    .ToList();

                var html = BuildHtml(items);

                ctx.Response.ContentType = "text/html";
                await ctx.Response.WriteAsync(html);
            }).ExcludeFromDescription();

            webApp.MapGet("/debug/{id}", async (HttpContext ctx, string id, RequestStore store) =>
            {
                var item = store.Get(id);

                if (item == null)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync("Not found");
                    return;
                }

                var prettyRequest = FormatJson(item.RequestBody);
                var prettyResponse = FormatJson(item.ResponseBody);

                var html = BuildDetailsHtml(item, prettyRequest, prettyResponse);

                ctx.Response.ContentType = "text/html";
                await ctx.Response.WriteAsync(html);
            }).ExcludeFromDescription();


            webApp.MapGet("/debug/compare/{id}", async (string id, string url, RequestStore store) =>
            {
                var local = store.Get(id);
                if (local == null) return Results.NotFound("Local not found");

                using var http = new HttpClient();

                var remote = await http.GetFromJsonAsync<DebugEntry>(url);
                if (remote == null) return Results.BadRequest("Remote not found");

                var diff = Compare(local, remote);

                return Results.Ok(diff);
            }).ExcludeFromDescription();


            webApp.MapGet("/debug/json/{id}", (string id, RequestStore store) =>
            {
                var item = store.GetAll().FirstOrDefault(x => x.Id == id);
                return item is null ? Results.NotFound() : Results.Json(item);
            }).ExcludeFromDescription();
        }

        return app;
    }

    private static string BuildHtml(List<DebugEntry> items)
    {
        var rows = string.Join("", items.Select(x => $@"
            <tr onclick=""window.location='/debug/{x.Id}'"" style=""cursor:pointer"">
                <td>{x.Timestamp:HH:mm:ss}</td>
                <td>{x.Method}</td>
                <td>{x.Path}</td>
                <td style=""color:{(x.StatusCode >= 400 ? "#e74c3c" : "#2ecc71")}; font-weight:bold;"">
                    {x.StatusCode}
                </td>
            </tr>"
        ));

        return $@"
            <html>
            <head>
                <title>DebugProbe</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        background:#f7f7f7;
                        padding:20px;
                    }}
                        
                    pre {{
                        max-height: 300px;
                        overflow: auto;
                        border-radius: 6px;
                    }}

                    h2 {{
                        margin-bottom:20px;
                    }}

                    h3 {{
                        margin - top: 25px;
                    }}

                    table {{
                        border-collapse: collapse;
                        width:100%;
                        background:white;
                    }}

                    th, td {{
                        padding:10px;
                        border-bottom:1px solid #eee;
                        text-align:left;
                    }}

                    th {{
                        background:#fafafa;
                    }}

                    tr:hover {{
                        background:#f1f1f1;
                    }}
                </style>
            </head>
            <body>
                <h2>DebugProbe</h2>

                <table>
                    <thead>
                        <tr>
                            <th>Time</th>
                            <th>Method</th>
                            <th>Path</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        {rows}
                    </tbody>
                </table>
            </body>
            </html>";
    }

    private static string BuildDetailsHtml(DebugEntry x, string req, string res)
    {
        var headers = string.Join("", x.Headers.Select(h =>
            $"<tr><td>{h.Key}</td><td>{h.Value}</td></tr>"));

        return $@"
            <html>
            <head>
                <title>DebugProbe Details</title>
                <style>
                    body {{ font-family: Arial; padding:20px; background:#f7f7f7; }}
                    pre {{ background:#1e1e1e; color:#dcdcdc; padding:10px; overflow:auto; }}
                    table {{ border-collapse: collapse; width:100%; margin-top:20px; background:white; }}
                    td {{ border:1px solid #eee; padding:6px; }}
                    h3 {{ margin-top:30px; }}
                    a {{ display:inline-block; margin-bottom:20px; }}
                </style>
            </head>
            <body>

            <a href=""/debug"">&#8592; Back</a>

            <h2>{x.Method} {x.Path}</h2>

            <p><b>Status:</b> {x.StatusCode}</p>
            <p><b>Time:</b> {x.Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC</p>
            <p><b>Local:</b> {x.Timestamp.ToLocalTime():HH:mm:ss}</p>
            <p><b>Env:</b> {x.Environment}</p>
            <p><b>Culture:</b> {x.Culture}</p>

            <h3>Request Body</h3>
            <pre>{System.Net.WebUtility.HtmlEncode(req)}</pre>

            <h3>Response Body</h3>
            <pre>{System.Net.WebUtility.HtmlEncode(res)}</pre>

            <h3>Headers</h3>
            <table>
            {headers}
            </table>

            <h3>Compare</h3>

            <input id=""compareUrl"" placeholder=""Paste remote /debug/json/{{id}} URL"" style=""width:400px;"" />
            <button onclick=""runCompare()"">Compare</button>

            <div id=""compareResult"" style=""margin-top:20px;""></div>

            </body>
            </html>"
            + @"
                <script>
                async function runCompare() {
                    const url = document.getElementById('compareUrl').value;
                    const id = window.location.pathname.split('/').pop();

                    if (!url) {
                        alert('Paste URL first');
                        return;
                    }

                    document.getElementById('compareResult').innerHTML =
                        '<b style=""color:orange"">Comparing...</b>';

                    try {
                        const res = await fetch('/debug/compare/' + id + '?url=' + encodeURIComponent(url));
                        const data = await res.json();

                        let html = '';

                        if (data.length === 0) {
                            html = '<b style=""color:green"">No differences</b>';
                        } else {
                            html += '<table style=""border-collapse:collapse;width:100%"">';
                            html += '<tr><th>Field</th><th>Local</th><th>Remote</th></tr>';

                            data.forEach(d => {
                                html += '<tr>'
                                    + '<td>' + d.field + '</td>'
                                    + '<td style=""color:#e74c3c"">' + d.local + '</td>'
                                    + '<td style=""color:#3498db"">' + d.remote + '</td>'
                                    + '</tr>';
                            });

                            html += '</table>';
                        }

                        document.getElementById('compareResult').innerHTML = html;
                    } catch {
                        document.getElementById('compareResult').innerHTML =
                            '<b style=""color:red"">Error during compare</b>';
                    }
                }
                </script>
                "
            ;
    }

    private static object Compare(DebugEntry a, DebugEntry b)
    {
        var diffs = new List<object>();

        // Basic fields
        if (a.StatusCode != b.StatusCode)
            diffs.Add(new { field = "StatusCode", local = a.StatusCode, remote = b.StatusCode });

        if (a.Environment != b.Environment)
            diffs.Add(new { field = "Environment", local = a.Environment, remote = b.Environment });

        if (a.Culture != b.Culture)
            diffs.Add(new { field = "Culture", local = a.Culture, remote = b.Culture });

        // ResponseBody (JSON-aware)
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
            diffs.Add(new { field = path, local = a.ToString(), remote = b.ToString() });
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

                if (aVal != bVal)
                {
                    diffs.Add(new
                    {
                        field = path,
                        local = aVal,
                        remote = bVal
                    });
                }
                break;
        }
    }

    private static string FormatJson(string json)
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

