using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Topaz.Service.Shared;

public sealed class YamlSerializerFacade
{
    static YamlSerializerFacade()
    {
        Deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    private static IDeserializer Deserializer { get; set; }
    
    public static T Deserialize<T>(string yaml)
    {
        return Deserializer.Deserialize<T>(yaml);
    }
}