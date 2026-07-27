using System.Text;
using System.Text.Json.Nodes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Topaz.Tests.AzureCLI;

/// <summary>
/// End-to-end test that validates the full "create ACR → az acr login → docker push" workflow
/// against a local Topaz instance.
///
/// The test uses three containers connected to a shared Docker network:
///   1. topaz.local.dev  — the Topaz emulator (control + registry planes)
///   2. azure-cli        — runs `az` commands against Topaz
///   3. docker-dind      — a privileged Docker-in-Docker daemon; runs `docker build` and
///                         `docker push` from inside the same network so the registry
///                         hostname resolves correctly without touching the host.
///
/// NOTE: This test is expected to fail until Topaz implements the Docker Registry V2
///       push endpoints (blob upload and manifest push).
/// </summary>
[TestFixture]
public class ContainerRegistryDockerPushTests
{
    private const string RegistryName = "topazacrpush01";
    private const string ResourceGroup = "test-acr-dockerpush-rg";
    private const string LoginServerHost = $"{RegistryName}.cr.topaz.local.dev";
    private const string LoginServer = $"{LoginServerHost}:8892";
    private const string RemoteImage = $"{LoginServer}/topaz-test-image:v1";

    // Docker-in-Docker: lightweight daemon running inside the network so it can reach the registry.
    private const string DindImage = "docker:27.5-dind";

    private const string AzureCliImage = "mcr.microsoft.com/azure-cli:2.84.0";

    private static readonly string TopazImage =
        Environment.GetEnvironmentVariable("TOPAZ_HOST_CONTAINER_IMAGE") ?? "topaz/host";

private static readonly string CertFile = File.ReadAllText("topaz.crt");
    private static readonly string CertKey  = File.ReadAllText("topaz.key");

    private const string CloudConfig = """
                                       {
                                         "endpoints": {
                                           "resourceManager":                  "https://topaz.local.dev:8899",
                                           "activeDirectory":                  "https://topaz.local.dev:8899",
                                           "activeDirectoryResourceId":        "https://topaz.local.dev:8899",
                                           "activeDirectoryGraphResourceId":   "https://topaz.local.dev:8899",
                                           "microsoft_graph_resource_id":      "https://topaz.local.dev:8899",
                                           "acr_login_server_endpoint":        "https://topaz.local.dev:8899"
                                         },
                                         "suffixes": {
                                           "keyvault_dns":          ".vault.topaz.local.dev",
                                           "acrLoginServerEndpoint": ".cr.topaz.local.dev"
                                         }
                                       }
                                       """;

    private INetwork?    _network;
    private IContainer?  _containerTopaz;
    private IContainer?  _containerAzureCli;
    private IContainer?  _containerDind;

    // ── Fixture setup ──────────────────────────────────────────────────────────

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        _containerTopaz = new ContainerBuilder()
            .WithImage(TopazImage)
            .WithPortBinding(8899)
            .WithNetwork(_network)
            .WithName("topaz.local.dev")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertFile), "/app/topaz.crt")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertKey),  "/app/topaz.key")
            .WithCommand(
                "--certificate-file", "topaz.crt",
                "--certificate-key",  "topaz.key",
                "--log-level",        "Debug",
                "--default-subscription", Guid.NewGuid().ToString())
            .Build();

        await _containerTopaz.StartAsync();
        await Task.Delay(TimeSpan.FromSeconds(3));

        // ── Docker-in-Docker ──────────────────────────────────────────────────
        // Privileged mode is required for dockerd to run inside a container.
        // TLS is disabled (DOCKER_TLS_CERTDIR="") for the daemon's own API socket
        // so that ExecAsync can connect without TLS.
        //
        // The Topaz self-signed cert is placed in certs.d BEFORE dockerd starts by
        // overriding the entrypoint with a shell script that runs first, then execs
        // the standard dockerd-entrypoint.sh. This avoids any race between the
        // post-start exec and the daemon's first TLS handshake with the registry.
        var certB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(CertFile));
        var certDir = $"/etc/docker/certs.d/{LoginServer}";
        var dindStartupScript =
            $"mkdir -p '{certDir}' && " +
            $"printf '%s' '{certB64}' | base64 -d > '{certDir}/ca.crt' && " +
            "exec dockerd-entrypoint.sh dockerd";

        _containerDind = new ContainerBuilder()
            .WithImage(DindImage)
            .WithNetwork(_network)
            .WithPrivileged(true)
            .WithEnvironment("DOCKER_TLS_CERTDIR", "")
            .WithExtraHost(LoginServerHost, _containerTopaz.IpAddress)
            .WithEntrypoint("/bin/sh", "-c")
            .WithCommand(dindStartupScript)
            .Build();

        await _containerDind.StartAsync();

        // Poll until the inner dockerd is responsive (up to 30 s).
        var dockerReady = false;
        for (var attempt = 0; attempt < 30 && !dockerReady; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var probe = await _containerDind.ExecAsync(["docker", "info"]);
            dockerReady = probe.ExitCode == 0;
        }

        if (!dockerReady)
            Assert.Fail("Docker daemon inside the DinD container did not become responsive within 30 seconds.");


        // ── Azure CLI ─────────────────────────────────────────────────────────
        _containerAzureCli = new ContainerBuilder()
            .WithImage(AzureCliImage)
            .WithNetwork(_network)
            .WithEntrypoint("/bin/sh")
            .WithCommand("-c", "tail -f /dev/null")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CloudConfig), "cloud.json")
            .WithResourceMapping(Encoding.UTF8.GetBytes(CertFile),    "/tmp/topaz.crt")
            .WithEnvironment("REQUESTS_CA_BUNDLE",
                "/usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem")
            .WithEnvironment("AZURE_CORE_INSTANCE_DISCOVERY", "false")
            .WithExtraHost(LoginServerHost, _containerTopaz.IpAddress)
            .Build();

        await _containerAzureCli.StartAsync();

        // Append Topaz cert to the Azure CLI CA bundle.
        var appendResult = await _containerAzureCli.ExecAsync(["sh", "-c",
            "cat /tmp/topaz.crt >> /usr/lib64/az/lib/python3.12/site-packages/certifi/cacert.pem"]);

        Assert.That(appendResult.ExitCode, Is.EqualTo(0),
            $"Failed to append Topaz cert. STDERR: {appendResult.Stderr}");

        // Register the Topaz cloud and log in.
        await RunAzureCliCommand("az cloud register -n Topaz --cloud-config @\"cloud.json\"");
        await RunAzureCliCommand("az cloud set -n Topaz");
        await RunAzureCliCommand("az login --username topazadmin@topaz.local.dev --password admin");
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_containerAzureCli is not null) await _containerAzureCli.DisposeAsync();
        if (_containerDind      is not null) await _containerDind.DisposeAsync();
        if (_containerTopaz     is not null) await _containerTopaz.DisposeAsync();
        if (_network            is not null) await _network.DisposeAsync();
    }

    // ── Test ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Full workflow: create an ACR registry in Topaz, authenticate with
    /// <c>az acr login</c>, build a local image inside the DinD daemon, and
    /// push it to the Topaz registry with <c>docker push</c>.
    ///
    /// Expected to FAIL until Topaz implements the Docker Registry V2
    /// blob-upload and manifest-push endpoints.
    /// </summary>
    [Test]
    public async Task ContainerRegistry_AcrLogin_And_DockerPush_ShouldUploadImage()
    {
        // ── Step 1: create the ACR registry ───────────────────────────────────
        await RunAzureCliCommand($"az group create -n {ResourceGroup} -l westeurope");
        await RunAzureCliCommand(
            $"az acr create --name {RegistryName} --resource-group {ResourceGroup} " +
            "--sku Basic --location westeurope --admin-enabled true",
            resp => Assert.That(resp["name"]!.GetValue<string>(), Is.EqualTo(RegistryName)));

        // ── Step 2: obtain an ACR exchange token via az acr login ─────────────
        // `--expose-token` returns the OAuth2 exchange token without requiring a
        // local Docker daemon in the Azure CLI container.
        var accessToken = string.Empty;
        await RunAzureCliCommand(
            $"az acr login --name {RegistryName} --expose-token",
            resp =>
            {
                accessToken = resp["accessToken"]!.GetValue<string>();
                Assert.That(accessToken, Is.Not.Null.And.Not.Empty,
                    "az acr login did not return an access token");
            });

        // ── Step 3: build the test image inside the DinD daemon ───────────────
        // The Dockerfile in Topaz.Tests.AzureCLI/docker/Dockerfile is the
        // canonical definition; its content is inlined here so the DinD container
        // does not need a volume mount or file copy.
        const string inlineDockerfile =
            "FROM alpine:3.20\n" +
            "LABEL description=\"Topaz ACR push test image\"\n" +
            "CMD [\"echo\", \"hello from topaz test image\"]";

        await RunDockerCommand(
            $"printf '{inlineDockerfile}' | docker build -t {RemoteImage} -");

        // ── Step 4: authenticate docker against the Topaz ACR ─────────────────
        // The ACR exchange token is used as the password; the username is the
        // sentinel value "00000000-0000-0000-0000-000000000000" as per the ACR
        // OAuth2 spec (token-based authentication).
        await RunDockerCommand(
            $"printf '%s' '{accessToken}' | " +
            $"docker login {LoginServer} -u 00000000-0000-0000-0000-000000000000 --password-stdin");

        // ── Step 5: push the image to the Topaz ACR ───────────────────────────
        // This step is expected to fail until Topaz implements:
        //   POST   /v2/{name}/blobs/uploads/
        //   PATCH  /v2/{name}/blobs/uploads/{uuid}
        //   PUT    /v2/{name}/blobs/uploads/{uuid}?digest=sha256:...
        //   PUT    /v2/{name}/manifests/{tag}
        await RunDockerCommand($"docker push {RemoteImage}");

        // ── Cleanup ───────────────────────────────────────────────────────────
        await RunAzureCliCommand(
            $"az acr delete --name {RegistryName} --resource-group {ResourceGroup} --yes");
        await RunAzureCliCommand($"az group delete -n {ResourceGroup} --yes");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task RunAzureCliCommand(
        string command,
        Action<JsonNode>? assertion = null,
        int expectedExitCode = 0)
    {
        var result = await _containerAzureCli!.ExecAsync(["/bin/sh", "-c", command]);

        Console.WriteLine($"[az] {command}");
        if (result.ExitCode == 0)
            Console.WriteLine($"STDOUT: {result.Stdout}");
        else
            await Console.Error.WriteLineAsync($"STDERR: {result.Stderr}");

        Assert.That(result.ExitCode, Is.EqualTo(expectedExitCode),
            $"`{command}` exited with {result.ExitCode}. STDOUT: {result.Stdout}, STDERR: {result.Stderr}");

        if (assertion is not null)
            assertion(JsonNode.Parse(result.Stdout)!);
    }

    private async Task RunDockerCommand(string command, int expectedExitCode = 0)
    {
        // Commands run inside the DinD container where DOCKER_HOST defaults to
        // the in-container Unix socket (/var/run/docker.sock).
        var result = await _containerDind!.ExecAsync(["/bin/sh", "-c", command]);

        Console.WriteLine($"[docker] {command}");
        if (result.ExitCode == 0)
            Console.WriteLine($"STDOUT: {result.Stdout}");
        else
            await Console.Error.WriteLineAsync($"STDERR: {result.Stderr}");

        Assert.That(result.ExitCode, Is.EqualTo(expectedExitCode),
            $"`{command}` exited with {result.ExitCode}. STDOUT: {result.Stdout}, STDERR: {result.Stderr}");
    }
}
