namespace Topaz.EventPipeline;

public interface IEventDefinition<out T>
{
    string Name { get; }
    T Data { get; }
}