using Azure.Core;
using Azure.ResourceManager.ServiceBus;
using DotNet.Testcontainers.Builders;
using MassTransit;
using Topaz.AspNetCore.Extensions;
using Topaz.Examples.MassTransit;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMassTransit(x =>
{
    x.UsingAzureServiceBus((context,cfg) =>
    {
        cfg.Host(TopazResourceHelpers.GetServiceBusConnectionString());
        cfg.ConfigureEndpoints(context);
    });
    
    x.AddHostedService<Worker>();
    x.AddConsumer<MessageConsumer>();
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    var container = new ContainerBuilder()
        .WithImage("thecloudtheory/topaz-cli:v1.0.229-alpha")
        .WithPortBinding(8890)
        .WithPortBinding(8899)
        .WithPortBinding(8898)
        .WithPortBinding(8897)
        .WithPortBinding(8891)
        .WithPortBinding(8889)
        .WithName("topaz.local.dev")
        .WithCommand("start", "--skip-dns-registration", "--log-level", "Debug")
        .Build();

    await container.StartAsync()
        .ConfigureAwait(false);

    await Task.Delay(5000);
    
    var subscriptionId = Guid.NewGuid();
    const string resourceGroupName = "rg-topaz-masstransit-example";
    
    await builder.Configuration.AddTopaz(subscriptionId)
        .AddSubscription(subscriptionId, "topaz-masstransit-example")
        .AddResourceGroup(subscriptionId, resourceGroupName, AzureLocation.WestEurope)
        .AddServiceBusNamespace(ResourceGroupIdentifier.From(resourceGroupName), ServiceBusNamespaceIdentifier.From("sbnamespace"),
            new ServiceBusNamespaceData(AzureLocation.WestEurope))
        .AddServiceBusQueue(ResourceGroupIdentifier.From(resourceGroupName), ServiceBusNamespaceIdentifier.From("sbnamespace"), "sbqueue", new ServiceBusQueueData());
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
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

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}