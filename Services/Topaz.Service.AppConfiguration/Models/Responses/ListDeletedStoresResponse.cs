using Topaz.Service.Shared;

namespace Topaz.Service.AppConfiguration.Models.Responses;

internal sealed class ListDeletedStoresResponse : TopazApiModel
{
    public IReadOnlyList<DeletedConfigurationStore> Value { get; init; } = [];
    public string? NextLink { get; init; }

    public static ListDeletedStoresResponse From(ConfigurationStoreFullResource[]? deletedResource)
    {
        if (deletedResource is null)
        {
            return new ListDeletedStoresResponse
            {
                Value = []
            };
        }
        
        return new ListDeletedStoresResponse
        {
            Value = deletedResource.Select(x => DeletedConfigurationStore.From(x)).ToList()
        };
    }
}

internal class DeletedConfigurationStore : TopazApiModel
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Type { get; init; }
    public DeletedConfigurationStoreProperties? Properties { get; init; }

    public static DeletedConfigurationStore From(ConfigurationStoreFullResource store)
    {
        return new DeletedConfigurationStore
        {
            Id = store.Id,
            Name = store.Name,
            Type = store.Type,
            Properties = DeletedConfigurationStoreProperties.From(store)
        };
    }
}

internal sealed class DeletedConfigurationStoreProperties
{
    public string? ConfigurationStoreId { get; init; }
    public string? Location { get; init; }
    public IDictionary<string, string>? Tags { get; init; }
    public DateTimeOffset? DeletionDate { get; init; }
    public DateTimeOffset? ScheduledPurgeDate { get; init; }
    public bool? PurgeProtectionEnabled { get; init; }

    public static DeletedConfigurationStoreProperties? From(ConfigurationStoreFullResource store)
    {
        return new DeletedConfigurationStoreProperties
        {
            ConfigurationStoreId = store.Id,
            Location = store.Location,
            Tags = store.Tags,
            DeletionDate = store.DeletionDate,
            ScheduledPurgeDate = store.ScheduledPurgeDate,
            PurgeProtectionEnabled = store.Properties.EnablePurgeProtection
        };
    }
}