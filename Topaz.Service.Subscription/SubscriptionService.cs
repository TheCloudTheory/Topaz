﻿using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Subscription;

public sealed class SubscriptionService(ITopazLogger logger) : IServiceDefinition
{
    public static string LocalDirectoryPath => ".subscription";
    
    public static IReadOnlyCollection<string>? Subresources => null;

    public string Name => "Subscription";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints => [
        new SubscriptionEndpoint(new ResourceProvider(logger), logger)
    ];
}
