using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Topaz.Example.Dotnet.Web;
using Topaz.Identity;
using Topaz.ResourceManager;

var builder = WebApplication.CreateBuilder(args);
var keyVaultName = builder.Configuration["Azure:KeyVaultName"]!;

IContainer? container = null;
if (builder.Environment.IsDevelopment())
{
    container = new ContainerBuilder()
        .WithImage("thecloudtheory/topaz-cli:v1.0.79-alpha")
        .WithPortBinding(8890)
        .WithPortBinding(8899)
        .WithPortBinding(8898)
        .WithPortBinding(8897)
        .WithPortBinding(8891)
        .Build();

    await container.StartAsync()
        .ConfigureAwait(false);

    await Task.Delay(5000);

    var subscriptionId = Guid.NewGuid();
    const string resourceGroupName = "rg-topaz-webapp-example";

    await builder.Configuration.AddTopaz(subscriptionId)
        .AddSubscription(subscriptionId, "topaz-webapp-example")
        .AddResourceGroup(subscriptionId, resourceGroupName)
        .AddKeyVault(resourceGroupName, keyVaultName, new Dictionary<string, string>
        {
            { "secret", "value" }
        });
}

builder.Configuration.AddAzureKeyVault(
    TopazResourceHelpers.GetKeyVaultEndpoint(keyVaultName), new AzureLocalCredential());

var app = builder.Build();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (IConfiguration configuration) =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithOpenApi();

app.Run();

if (app.Environment.IsDevelopment() && container != null)
{
    await container.DisposeAsync();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}