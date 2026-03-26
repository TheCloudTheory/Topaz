namespace Topaz.Service.Storage.Exceptions;

[Serializable]
internal class InvalidInputException : Exception
{
    public InvalidInputException()
    {
    }

    public InvalidInputException(string? message) : base(message)
    {
    }

    public InvalidInputException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}