namespace Azure.Local.Service.Shared;

public interface IEndpointDefinition
{
    public Protocol Protocol { get; }
    public string DnsName { get; }
    public HttpResponseMessage GetResponse(string path, Stream input);
}
