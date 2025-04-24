using System.Net;
using System.Text;
using Azure.Local.Service.Shared;
using Azure.Local.Service.Storage;
using Azure.Local.Shared;

namespace Azure.Local.Host;

public class Host
{
    private static List<Thread> threads = [];

    public void Start()
    {
        var services = new [] { new AzureStorageService() };

        foreach(var service in services)
        {
            PrettyLogger.LogInformation($"Starting {service.Name} service...");

            foreach(var endpoint in service.Endpoints)
            {
                if(endpoint.Protocol == Service.Shared.Protocol.Http || endpoint.Protocol == Service.Shared.Protocol.Https)
                {
                    CreateHttpListenerForEndpoint(endpoint);
                }
            }
        }
    }

    private void CreateHttpListenerForEndpoint(IEndpointDefinition endpoint)
    {
        var action = new ThreadStart(() => {
            var prefix = $"{endpoint.Protocol}://localhost:{endpoint.PortNumber}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            listener.Start();
            PrettyLogger.LogInformation($"Started listening on {prefix}");

            while(true)
            {
                var context = listener.GetContext();
                var response = endpoint.GetResponse(context.Request.InputStream);
                var buffer = Encoding.UTF8.GetBytes(response);
                
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }    
        });

        var thread = new Thread(action);

        threads.Add(thread);
        thread.Start();
    }
}
