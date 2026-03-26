namespace Topaz.Service.Authorization.Domain;

public record RoleAssignmentName(Guid Value)
{
    public static RoleAssignmentName From(Guid? value)
    {
        return !value.HasValue ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value)) : new RoleAssignmentName(value.Value);
    }
    
    public static RoleAssignmentName From(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(value)) : new RoleAssignmentName(Guid.Parse(value));
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}