namespace Azure.Local.Service.Shared;

public interface IEndpointDefinition
{
    public Protocol Protocol { get; }
    public string DnsName { get; }
    public string GetResponse(Stream input);
}
