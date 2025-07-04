﻿using Topaz.CloudEnvironment.Endpoints;
using Topaz.Service.Shared;

namespace Topaz.CloudEnvironment;

public sealed class TopazCloudEnvironmentService : IServiceDefinition
{
    public string Name => "CloudEnvironment";
    public static string LocalDirectoryPath => ".cloud";
    public static IReadOnlyCollection<string>? Subresources => null;

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new MetadataEndpoint(),
        new TenantsEndpoint()
    ];
}