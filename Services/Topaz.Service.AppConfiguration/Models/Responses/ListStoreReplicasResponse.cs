using JetBrains.Annotations;
using Topaz.Service.Shared;

namespace Topaz.Service.AppConfiguration.Models.Responses;

internal sealed class ListStoreReplicasResponse : TopazApiModel
{
    public ReplicaResource[]? Value { [UsedImplicitly] get; set; }

    public static ListStoreReplicasResponse From(ReplicaResource[] replicas)
    {
        return new ListStoreReplicasResponse { Value = replicas };
    }
}