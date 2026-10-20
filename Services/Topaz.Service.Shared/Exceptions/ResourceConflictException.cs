namespace Topaz.Service.Shared.Exceptions;

/// <summary>
/// Represents an exception thrown when a conflict occurs during a resource operation.
/// </summary>
public class ResourceConflictException(string message) : Exception(message);