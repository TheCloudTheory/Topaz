namespace Topaz.Service.Shared;

public sealed class CorrelationIdFactory
{
    private Guid? _correlationId;

    public Guid Get()
    {
        _correlationId ??= Guid.NewGuid();

        return _correlationId.Value;
    }
}