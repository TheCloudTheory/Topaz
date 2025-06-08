using DotNet.Testcontainers.Builders;

Console.WriteLine("Topaz Example - .NET");

// Create a builder for Topaz container image and all the ports
// which are exposed by the emulator. Note you don't need to expose
// every port available.
var container = new ContainerBuilder()
    .WithImage("thecloudtheory/topaz-cli:v1.0.61-alpha")
    .WithPortBinding(8890)
    .WithPortBinding(8899)
    .WithPortBinding(8898)
    .WithPortBinding(8897)
    .WithPortBinding(8891)
    .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8890).ForPort(8899).ForPort(8898).ForPort(8897).ForPort(8891)))
    .Build();
    
await container.StartAsync()
    .ConfigureAwait(false);
    
    