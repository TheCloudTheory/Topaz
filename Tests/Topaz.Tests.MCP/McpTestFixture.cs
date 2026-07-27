using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using NUnit.Framework;
using Topaz.Identity;
using Topaz.ResourceManager;
using Topaz.Service.Shared;
using Topaz.Shared;
using TopazHost = Topaz.Host.Host;

namespace Topaz.Tests.MCP;

[SetUpFixture]
public class McpTestFixture
{
    internal static readonly Guid SubscriptionId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    internal const string ObjectId = "00000000-0000-0000-0000-000000000000";
    internal const string ResourceGroupName = "rg-mcp-tests";
    internal static readonly AzureLocation Location = AzureLocation.EastUS;
    internal static ArmClientOptions ArmClientOptions => TopazArmClientOptions.New;

    private static Task? _topaz;
    private static readonly CancellationTokenSource CancellationTokenSource = new();

    [OneTimeSetUp]
    public async Task SetUp()
    {
        var logger = new PrettyTopazLogger();
        var logLevel = Environment.GetEnvironmentVariable("CI") == "true" ? LogLevel.Information : LogLevel.Debug;
        logger.SetLoggingLevel(logLevel);
        logger.EnableLoggingToFile(true);

        var host = new TopazHost(new GlobalOptions
        {
            EnableLoggingToFile = true,
            EmulatorIpAddress = "127.0.0.1"
        }, logger);

        _topaz = host.StartAsync(CancellationTokenSource.Token);

        await Task.Delay(1000);

        var credentials = new AzureLocalCredential(ObjectId);
        using var topaz = new TopazArmClient(credentials);
        await topaz.CreateSubscriptionAsync(SubscriptionId, "mcp-test-subscription");

        var armClient = new ArmClient(credentials, SubscriptionId.ToString(), ArmClientOptions);
        var subscription = await armClient.GetDefaultSubscriptionAsync();
        await subscription.GetResourceGroups().CreateOrUpdateAsync(
            WaitUntil.Completed,
            ResourceGroupName,
            new ResourceGroupData(Location));
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        await CancellationTokenSource.CancelAsync();

        if (_topaz != null)
        {
            try
            {
                await _topaz.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
        }

        CancellationTokenSource.Dispose();
    }
}
