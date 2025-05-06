namespace Azure.Local.Service.Storage.Exceptions;

[Serializable]
internal class UpdateConditionNotSatisfiedException : Exception
{
    public UpdateConditionNotSatisfiedException()
    {
    }

    public UpdateConditionNotSatisfiedException(string? message) : base(message)
    {
    }

    public UpdateConditionNotSatisfiedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}