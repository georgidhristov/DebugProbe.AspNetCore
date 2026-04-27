using DebugProbe.AspNetCore.Models;

namespace DebugProbe.AspNetCore.Internal;

/// <summary>
/// Renders DebugProbe UI pages (layout, index, details) using embedded HTML templates.
/// </summary>
internal class HtmlRenderer
{
    public static string Env { get; } = EnvironmentUtils.TryGetEnvironment();

    public static string BuildLayout(string content)
    {
        var envBlock = string.IsNullOrWhiteSpace(Env) ? "" : $"<span class=\"env\">{System.Net.WebUtility.HtmlEncode(Env)}</span>";

        return EmbeddedResources.Layout
            .Replace("{{styles}}", $"<style>{EmbeddedResources.Css}</style>")
            .Replace("{{content}}", content)
            .Replace("{{env_block}}", envBlock);
    }

    public static string RenderIndexPage(List<DebugEntry> items)
    {
        var rows = string.Join("", items.Select(x => $@"
        <tr onclick=""window.location='/debug/{x.Id}'"" style=""cursor:pointer"">
            <td>{x.Timestamp:HH:mm:ss}</td>
            <td>{System.Net.WebUtility.HtmlEncode(x.Method)}</td>
            <td>{System.Net.WebUtility.HtmlEncode(x.Path)}</td>
            <td style=""color:{(x.StatusCode >= 400 ? "#e74c3c" : "#2ecc71")}; font-weight:bold;"">
                {x.StatusCode}
            </td>
        </tr>"
        ));

        if (string.IsNullOrEmpty(rows))
            rows = "<tr><td colspan='4'>No data</td></tr>";

        var content = EmbeddedResources.Index.Replace("{{rows}}", rows);

        return BuildLayout(content);
    }

    public static string RenderDetailsPage(DebugEntry x, string req, string res)
    {
        var headers = string.Join("", x.Headers.Select(h =>
            $"<tr><td>{h.Key}</td><td>{h.Value}</td></tr>"));

        var pathWithQuery = string.IsNullOrEmpty(x.Query)
            ? x.Path
            : $"{x.Path}{x.Query}";

        var content = EmbeddedResources.Details
            .Replace("{{method}}", x.Method)
            .Replace("{{path}}", pathWithQuery)
            .Replace("{{status}}", x.StatusCode.ToString())
            .Replace("{{time}}", x.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"))
            .Replace("{{local}}", x.Timestamp.ToLocalTime().ToString("HH:mm:ss"))
            .Replace("{{env}}", x.Environment)
            .Replace("{{culture}}", x.Culture)
            .Replace("{{requestUrl}}", System.Net.WebUtility.HtmlEncode(string.IsNullOrEmpty(x.RequestUrl) ? "(empty)" : x.RequestUrl))
            .Replace("{{request}}", System.Net.WebUtility.HtmlEncode(string.IsNullOrEmpty(req) ? "(empty)" : req))
            .Replace("{{response}}", System.Net.WebUtility.HtmlEncode(string.IsNullOrEmpty(res) ? "(empty)" : res))
            .Replace("{{headers}}", headers);

        return BuildLayout(content);
    }
}
