namespace Topaz.Service.Shared.Exceptions;

internal sealed class TopazQuotaExceededException(string message) : Exception(message);