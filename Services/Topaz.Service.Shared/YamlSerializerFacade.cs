using JetBrains.Annotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Topaz.Service.Shared;

[UsedImplicitly]
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
        try
        {
            return Deserializer.Deserialize<T>(yaml);
        }
        catch (YamlException ex)
        {
            throw new WrappedYamlException(ex);
        }
    }
}

public class WrappedYamlException : Exception
{
    internal WrappedYamlException(YamlException exception)
        : base(exception.Message, exception)
    {
    }
}