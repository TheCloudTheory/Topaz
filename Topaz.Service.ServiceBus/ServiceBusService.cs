﻿using Topaz.Service.ResourceGroup;
using Topaz.Service.ServiceBus.Endpoints;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ServiceBus;

public sealed class ServiceBusService(ITopazLogger logger) : IServiceDefinition
{
    public string Name => "Azure Service Bus";
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => Path.Combine(ResourceGroupService.LocalDirectoryPath, ".service-bus");
    public static IReadOnlyCollection<string>? Subresources => [nameof(Subresource.Queues).ToLowerInvariant(), nameof(Subresource.Topics).ToLowerInvariant()];

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new ServiceBusEndpoint(),
        new ServiceBusServiceEndpoint(logger)
    ];
}