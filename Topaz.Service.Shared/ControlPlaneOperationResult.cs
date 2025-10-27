namespace Topaz.Service.Shared;

public record ControlPlaneOperationResult<TResource>(OperationResult Result, TResource? Resource, string? Reason, string? Code);