using System.Net;
using LMDashboard.Components;
using LMDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<LinkStore>();

static void ConfigurePingClient(HttpClient client)
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("LMDashboard/1.0");
}

// Neither client follows redirects, so a 3xx is reported as REDIR rather than as the
// status of whatever page it lands on.

// Internal hosts often run self-signed certs, so validation is skipped there.
builder.Services.AddHttpClient("PingInternal", ConfigurePingClient)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });

// External links keep full certificate validation so cert problems surface as failures.
builder.Services.AddHttpClient("PingExternal", ConfigurePingClient)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

builder.Services.AddHostedService<PingService>();

var app = builder.Build();

// Only answer requests addressed to a known host name or an IP inside an allowed network.
// This blocks DNS rebinding, where a web page re-points its own domain at this server.
var hostAccess = app.Configuration.GetSection("HostAccess");
var allowedHostNames = hostAccess.GetSection("Hosts").Get<string[]>() ?? [];
var allowedNetworks = (hostAccess.GetSection("Networks").Get<string[]>() ?? [])
    .Select(IPNetwork.Parse)
    .ToArray();

app.Use(async (context, next) =>
{
    var host = context.Request.Host.Host.Trim('[', ']');
    var allowed = allowedHostNames.Contains(host, StringComparer.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host, out var ip) && allowedNetworks.Any(n => n.Contains(ip)));

    if (!allowed)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    // Stop other sites from framing the dashboard and tricking clicks on [DEL].
    // Blazor sets the matching CSP frame-ancestors header; see AddInteractiveServerRenderMode below.
    context.Response.Headers.XFrameOptions = "DENY";
    await next();
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = "'none'");

app.Run();
