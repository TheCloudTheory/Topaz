namespace Topaz.Service.Shared.Domain;

public record ResourceGroupIdentifier(string Value)
{
    public static ResourceGroupIdentifier From(string value)
    {
        return new ResourceGroupIdentifier(value);
    }

    public override string ToString()
    {
        return Value;
    }
}