using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Topaz.Portal;
using Topaz.Portal.Components;

var builder = WebApplication.CreateBuilder(args);

// Configure HTTPS with the same certificate as Topaz.Host
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(listenOptions =>
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // On macOS, use the PFX certificate
            if (File.Exists("topaz.pfx"))
            {
                listenOptions.ServerCertificate = new X509Certificate2("topaz.pfx", "qwerty");
            }
        }
        else
        {
            // On other platforms, use PEM certificate
            string? certPath = null;
            string? keyPath = null;
            
            if (File.Exists("topaz.crt") && File.Exists("topaz.key"))
            {
                certPath = "topaz.crt";
                keyPath = "topaz.key";
            }

            if (certPath == null || keyPath == null) return;
            
            var certPem = File.ReadAllText(certPath);
            var keyPem = File.ReadAllText(keyPath);
            listenOptions.ServerCertificate = X509Certificate2.CreateFromPem(certPem, keyPem);
        }
    });
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<TopazClient>();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AccountSession>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();