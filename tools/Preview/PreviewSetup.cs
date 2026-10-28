using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

public static class PreviewSetup
{
    public static string Root(string marker)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !Directory.Exists(Path.Combine(root.FullName, marker))) root = root.Parent;
        return root?.FullName ?? throw new InvalidOperationException("Run this preview inside its repository.");
    }
    public static WebApplicationBuilder Builder(string root, int port, string? webRoot = null, string? applicationName = null)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production", ContentRootPath = Path.Combine(root, "tools", "Preview"), WebRootPath = webRoot, ApplicationName = applicationName });
        builder.Configuration.Sources.Clear(); builder.Configuration.AddInMemoryCollection();
        builder.Logging.ClearProviders(); builder.Logging.AddSimpleConsole();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        return builder;
    }
    public static void Guard(WebApplication app, bool decorate = false)
    {
        app.Use(async (context, next) =>
        {
            if (!HttpMethods.IsGet(context.Request.Method)) { context.Response.StatusCode = 405; return; }
            context.Response.Headers["Cache-Control"] = "no-store";
            if (!decorate) { await next(); return; }
            var original = context.Response.Body; using var buffer = new MemoryStream(); context.Response.Body = buffer;
            try
            {
                await next(); buffer.Position = 0; context.Response.Body = original;
                if (context.Response.ContentType?.StartsWith("text/html") == true)
                {
                    var html = await new StreamReader(buffer).ReadToEndAsync();
                    html = Regex.Replace(html, @"<script[^>]+src=['""]https://cdn\.tiny\.cloud/[^'""]+['""][^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    html = html.Replace("<body>", "<body><aside style='padding:12px;background:#173641;color:white;font:14px system-ui;text-align:center'>READ-ONLY FIXTURE · Existing views · Sample data · No database or providers. Only the displayed preview routes work.</aside>");
                    await context.Response.WriteAsync(html);
                }
                else await buffer.CopyToAsync(original);
            }
            finally { context.Response.Body = original; }
        });
        app.MapGet("/preview-info", () => new { mode = "read-only fixture", database = false, externalProviders = false, originalHost = false });
    }
}

public static class EvidencePage
{
    public static string Html(string title, string intro, object evidence, string? visual = null)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(evidence, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var data = System.Net.WebUtility.HtmlEncode(json);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var rows = new System.Text.StringBuilder();
        foreach (var item in document.RootElement.EnumerateArray())
        {
            var label = item.TryGetProperty("scenario", out var scenario) ? scenario.GetString() : item.GetProperty("path").GetString();
            rows.Append("<tr><td>").Append(System.Net.WebUtility.HtmlEncode(label)).Append("</td><td>GET</td><td><strong>").Append(item.GetProperty("status").GetInt32()).Append("</strong></td></tr>");
        }
        var summary = "<table><thead><tr><th>Scenario / route</th><th>Method</th><th>Observed status</th></tr></thead><tbody>" + rows + "</tbody></table>";
        return "<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>" + title + " · local evidence</title><style>body{margin:0;background:#010409;color:#dce6ef;font:16px/1.6 system-ui}main{max-width:1050px;margin:28px auto;padding:20px}table{width:100%;border-collapse:collapse}td,th{text-align:left;padding:14px;border-bottom:1px solid #30363d}strong{color:#69cbd8;font-size:26px}summary{cursor:pointer;color:#69cbd8}small{color:#69cbd8}h1{font-size:36px;margin:8px 0}p{color:#9eacba}.card{background:#0d1117;border:1px solid #30363d;border-radius:12px;padding:24px;margin-top:24px}pre{white-space:pre-wrap;overflow-wrap:anywhere;font:15px/1.65 Consolas,monospace;color:#b9e9da}a{color:#69cbd8}img{max-width:100%;border:1px solid #30363d}section{display:flex;gap:25px;align-items:start;flex-wrap:wrap}figure{margin:0;flex:1;min-width:260px}</style><main><small>LOCAL FIXTURE EVIDENCE · No database / cloud / original host</small><h1>" + title + "</h1><p>" + intro + "</p>" + (visual ?? "") + "<div class='card'><h2>Observed results</h2>" + summary + "<details><summary>Inspect response bodies</summary><pre>" + data + "</pre></details></div><p><a href='/evidence.json'>Machine-readable results</a> · Existing implementation with fixture inputs. No live integration result is implied.</p></main></html>";
    }
}
