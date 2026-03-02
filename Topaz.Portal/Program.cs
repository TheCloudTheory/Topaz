using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Topaz.Portal;
using Topaz.Portal.Components;

var builder = WebApplication.CreateBuilder(args);

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