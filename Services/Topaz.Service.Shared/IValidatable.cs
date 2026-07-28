namespace Topaz.Service.Shared;

public interface IValidatable
{
    (bool IsValid, string? Error) Validate<TModel>(TModel? data = null) where TModel : class;
}