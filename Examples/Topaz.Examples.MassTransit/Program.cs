using System.Text;
using Azure.Core;
using Azure.ResourceManager.ServiceBus;
using DotNet.Testcontainers.Builders;
using MassTransit;
using Topaz.AspNetCore.Extensions;
using Topaz.Examples.MassTransit;
using Topaz.ResourceManager;
using Topaz.Service.Shared.Domain;

var topazContainerImage = Environment.GetEnvironmentVariable("TOPAZ_CLI_CONTAINER_IMAGE") == null ? 
    "topaz/cli"
    : Environment.GetEnvironmentVariable("TOPAZ_CLI_CONTAINER_IMAGE")!;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    var certificateFile = File.ReadAllText("topaz.crt");
    var certificateKey = File.ReadAllText("topaz.key");

    var container = new ContainerBuilder(topazContainerImage)
        .WithPortBinding(443)
        .WithPortBinding(5671)  // AMQPS (AMQP with TLS) - for MassTransit
        .WithPortBinding(8889)  // Plain AMQP - for Azure SDK with UseDevelopmentEmulator=true
        .WithPortBinding(8890)
        .WithPortBinding(8899)
        .WithPortBinding(8898)
        .WithPortBinding(8897)
        .WithPortBinding(8891)
        .WithHostname("topaz.local.dev")
        .WithResourceMapping(Encoding.UTF8.GetBytes(certificateFile), "/app/topaz.crt")
        .WithResourceMapping(Encoding.UTF8.GetBytes(certificateKey), "/app/topaz.key")
        .WithCommand("start", "--certificate-file", "topaz.crt", "--certificate-key", "topaz.key", "--log-level", "Debug")
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
    
    // Give additional time for the queue to be fully registered with AMQP broker
    await Task.Delay(2000);
    
    // Test direct connection first
    Console.WriteLine("=== Testing direct Azure Service Bus SDK connection ===");
    await DirectTest.TestDirectConnection();
    Console.WriteLine("=== Direct test completed ===\n");
}

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MessageConsumer>();
    
    x.UsingAzureServiceBus((context, cfg) =>
    {
        // Use TLS connection string for MassTransit (port 5671 with TLS)
        var connectionString = TopazResourceHelpers.GetServiceBusConnectionStringWithTls("sbnamespace");
        
        // Configure with explicit transport type
        cfg.Host(connectionString, h =>
        {
            h.TransportType = Azure.Messaging.ServiceBus.ServiceBusTransportType.AmqpTcp;
        });
        
        // Add retry configuration for connection issues
        cfg.UseMessageRetry(r => r.Immediate(5));
        
        // Manually configure the receive endpoint to use the pre-created queue
        cfg.ReceiveEndpoint("sbqueue", e =>
        {
            // Configure consumer
            e.ConfigureConsumer<MessageConsumer>(context);
            
            // Add some buffer for connection establishment  
            e.PrefetchCount = 1;
        });
    });
});

// Add worker as regular hosted service (not via MassTransit)
builder.Services.AddHostedService<Worker>();

// Configure MassTransit to not wait for bus start - avoids validation errors
builder.Services.AddOptions<MassTransitHostOptions>()
    .Configure(options =>
    {
        options.WaitUntilStarted = false;  // Don't block on startup validation
        options.StartTimeout = TimeSpan.FromSeconds(10);
        options.StopTimeout = TimeSpan.FromSeconds(10);
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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