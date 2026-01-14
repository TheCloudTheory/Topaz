namespace Topaz.Shared;

public sealed class CorrelationIdFactory
{
    private Guid? _correlationId;

    public Guid Get()
    {
        _correlationId ??= Guid.NewGuid();

        return _correlationId.Value;
    }

    public void GenerateNew()
    {
        _correlationId = Guid.NewGuid();
    }
}