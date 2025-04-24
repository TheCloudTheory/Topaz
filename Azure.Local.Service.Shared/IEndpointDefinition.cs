namespace Azure.Local.Service.Shared;

public interface IEndpointDefinition
{
    public int PortNumber { get; }
    public Protocol Protocol { get; }
    public string GetResponse(Stream input);
}
