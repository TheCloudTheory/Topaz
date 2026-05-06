using Topaz.Service.ManagementGroup.Models;
using Topaz.Service.ManagementGroup.Models.Requests;
using Topaz.Service.ManagementGroup.Models.Responses;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.ManagementGroup;

internal sealed class ManagementGroupControlPlane(ManagementGroupResourceProvider provider, ITopazLogger logger)
{
    private const string NotFoundCode = "ManagementGroupNotFound";
    private const string NotFoundMessageTemplate = "Management group '{0}' not found.";
    private const string HasChildrenCode = "ManagementGroupHasChildren";
    private const string HasChildrenMessage = "Management group '{0}' cannot be deleted because it contains child management groups.";
    private const string ParentNotFoundCode = "ParentManagementGroupNotFound";
    private const string ParentNotFoundMessageTemplate = "Parent management group '{0}' not found.";

    public static ManagementGroupControlPlane New(ITopazLogger logger) =>
        new(new ManagementGroupResourceProvider(logger), logger);

    public ControlPlaneOperationResult<Models.ManagementGroup> Get(string groupId)
    {
        var mg = provider.GetManagementGroup(groupId);
        if (mg == null)
            return new ControlPlaneOperationResult<Models.ManagementGroup>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId), NotFoundCode);

        return new ControlPlaneOperationResult<Models.ManagementGroup>(OperationResult.Success, mg, null, null);
    }

    public ControlPlaneOperationResult<Models.ManagementGroup> CreateOrUpdate(
        string groupId, CreateOrUpdateManagementGroupRequest request)
    {
        var existing = provider.GetManagementGroup(groupId);
        var isNew = existing == null;

        var displayName = request.Properties?.DisplayName ?? groupId;

        ParentGroupInfo? parentInfo = null;
        var parentId = request.Properties?.Details?.Parent?.Id;
        if (!string.IsNullOrWhiteSpace(parentId))
        {
            var parentGroupId = ExtractGroupIdFromArmId(parentId);
            var parentMg = provider.GetManagementGroup(parentGroupId);
            if (parentMg == null)
                return new ControlPlaneOperationResult<Models.ManagementGroup>(OperationResult.Failed, null,
                    string.Format(ParentNotFoundMessageTemplate, parentGroupId), ParentNotFoundCode);

            parentInfo = new ParentGroupInfo
            {
                Id = parentMg.Id,
                Name = parentMg.Name,
                DisplayName = parentMg.Properties.DisplayName
            };
        }

        Models.ManagementGroup mg;
        if (isNew)
        {
            mg = Models.ManagementGroup.Create(groupId, displayName, parentInfo);
        }
        else
        {
            mg = existing!;
            mg.UpdateFrom(displayName, parentInfo);
        }

        provider.SaveManagementGroup(groupId, mg);

        var result = isNew ? OperationResult.Created : OperationResult.Updated;
        logger.LogDebug(nameof(ManagementGroupControlPlane), nameof(CreateOrUpdate),
            "{0} management group '{1}'.", isNew ? "Created" : "Updated", groupId);

        return new ControlPlaneOperationResult<Models.ManagementGroup>(result, mg, null, null);
    }

    public ControlPlaneOperationResult<Models.ManagementGroup> Update(
        string groupId, UpdateManagementGroupRequest request)
    {
        var existing = provider.GetManagementGroup(groupId);
        if (existing == null)
            return new ControlPlaneOperationResult<Models.ManagementGroup>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId), NotFoundCode);

        ParentGroupInfo? parentInfo = null;
        if (!string.IsNullOrWhiteSpace(request.Properties?.ParentGroupId))
        {
            var parentMg = provider.GetManagementGroup(request.Properties.ParentGroupId);
            if (parentMg == null)
                return new ControlPlaneOperationResult<Models.ManagementGroup>(OperationResult.Failed, null,
                    string.Format(ParentNotFoundMessageTemplate, request.Properties.ParentGroupId),
                    ParentNotFoundCode);

            parentInfo = new ParentGroupInfo
            {
                Id = parentMg.Id,
                Name = parentMg.Name,
                DisplayName = parentMg.Properties.DisplayName
            };
        }

        existing.UpdateFrom(request.Properties?.DisplayName, parentInfo);
        provider.SaveManagementGroup(groupId, existing);

        return new ControlPlaneOperationResult<Models.ManagementGroup>(OperationResult.Updated, existing, null, null);
    }

    public ControlPlaneOperationResult Delete(string groupId)
    {
        if (provider.GetManagementGroup(groupId) == null)
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                string.Format(NotFoundMessageTemplate, groupId), NotFoundCode);

        if (provider.HasChildren(groupId))
            return new ControlPlaneOperationResult(OperationResult.Failed,
                string.Format(HasChildrenMessage, groupId), HasChildrenCode);

        provider.DeleteManagementGroup(groupId);
        logger.LogDebug(nameof(ManagementGroupControlPlane), nameof(Delete),
            "Deleted management group '{0}'.", groupId);

        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<Models.ManagementGroup[]> List()
    {
        var groups = provider.ListManagementGroups().ToArray();
        return new ControlPlaneOperationResult<Models.ManagementGroup[]>(OperationResult.Success, groups, null, null);
    }

    public ControlPlaneOperationResult<EntityInfo[]> GetEntities()
    {
        var groups = provider.ListManagementGroups().ToArray();
        var subscriptions = provider.ListAllSubscriptionAssociations().ToArray();

        var entities = new List<EntityInfo>(groups.Length + subscriptions.Length);

        foreach (var mg in groups)
        {
            EntityParentInfo? parent = mg.Properties.Details.Parent != null
                ? new EntityParentInfo { Id = mg.Properties.Details.Parent.Id }
                : null;

            var children = groups.Count(g =>
                g.Properties.Details.Parent != null &&
                g.Properties.Details.Parent.Id == mg.Id);

            var subsForGroup = subscriptions.Count(s =>
                s.Properties.Parent?.Id == mg.Id);

            entities.Add(new EntityInfo
            {
                Id = mg.Id,
                Type = "Microsoft.Management/managementGroups",
                Name = mg.Name,
                Properties = new EntityInfoProperties
                {
                    DisplayName = mg.Properties.DisplayName,
                    Parent = parent,
                    NumberOfChildGroups = children,
                    NumberOfChildren = children + subsForGroup,
                    NumberOfDescendants = children + subsForGroup,
                    ParentNameChain = parent != null
                        ? [ExtractGroupIdFromArmId(parent.Id)]
                        : [],
                    ParentDisplayNameChain = parent != null && mg.Properties.Details.Parent != null
                        ? [mg.Properties.Details.Parent.DisplayName]
                        : []
                }
            });
        }

        foreach (var sub in subscriptions)
        {
            var hasParent = !string.IsNullOrEmpty(sub.Properties.Parent.Id);
            EntityParentInfo? parent = hasParent
                ? new EntityParentInfo { Id = sub.Properties.Parent.Id }
                : null;

            entities.Add(new EntityInfo
            {
                Id = $"/subscriptions/{sub.Name}",
                Type = "/subscriptions",
                Name = sub.Name,
                Properties = new EntityInfoProperties
                {
                    DisplayName = sub.Properties.DisplayName,
                    Parent = parent,
                    ParentNameChain = hasParent
                        ? [sub.Properties.Parent.Name]
                        : [],
                    ParentDisplayNameChain = hasParent
                        ? [groups.FirstOrDefault(g => g.Name == sub.Properties.Parent.Name)?.Properties.DisplayName ?? sub.Properties.Parent.Name]
                        : []
                }
            });
        }

        return new ControlPlaneOperationResult<EntityInfo[]>(OperationResult.Success, [.. entities], null, null);
    }

    public ControlPlaneOperationResult<Models.ManagementGroupSubscription> AssociateSubscription(
        string groupId, string subscriptionId)
    {
        if (provider.GetManagementGroup(groupId) == null)
            return new ControlPlaneOperationResult<Models.ManagementGroupSubscription>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId), NotFoundCode);

        var displayName = subscriptionId;
        var model = Models.ManagementGroupSubscription.Create(groupId, subscriptionId, displayName);
        provider.SaveSubscriptionAssociation(groupId, subscriptionId, model);

        logger.LogDebug(nameof(ManagementGroupControlPlane), nameof(AssociateSubscription),
            "Associated subscription '{0}' with management group '{1}'.", subscriptionId, groupId);

        return new ControlPlaneOperationResult<Models.ManagementGroupSubscription>(
            OperationResult.Updated, model, null, null);
    }

    public ControlPlaneOperationResult DisassociateSubscription(string groupId, string subscriptionId)
    {
        if (provider.GetManagementGroup(groupId) == null)
            return new ControlPlaneOperationResult(OperationResult.NotFound,
                string.Format(NotFoundMessageTemplate, groupId), NotFoundCode);

        provider.DeleteSubscriptionAssociation(groupId, subscriptionId);

        logger.LogDebug(nameof(ManagementGroupControlPlane), nameof(DisassociateSubscription),
            "Disassociated subscription '{0}' from management group '{1}'.", subscriptionId, groupId);

        return new ControlPlaneOperationResult(OperationResult.Deleted, null, null);
    }

    public ControlPlaneOperationResult<Models.ManagementGroupSubscription> GetSubscriptionUnderManagementGroup(
        string groupId, string subscriptionId)
    {
        if (provider.GetManagementGroup(groupId) == null)
            return new ControlPlaneOperationResult<Models.ManagementGroupSubscription>(OperationResult.NotFound, null,
                string.Format(NotFoundMessageTemplate, groupId), NotFoundCode);

        var association = provider.GetSubscriptionAssociation(groupId, subscriptionId);
        if (association == null)
            return new ControlPlaneOperationResult<Models.ManagementGroupSubscription>(OperationResult.NotFound, null,
                $"Subscription '{subscriptionId}' is not associated with management group '{groupId}'.",
                "SubscriptionNotFound");

        return new ControlPlaneOperationResult<Models.ManagementGroupSubscription>(
            OperationResult.Success, association, null, null);
    }

    private static string ExtractGroupIdFromArmId(string armId)
    {
        // Expect /providers/Microsoft.Management/managementGroups/{groupId}
        var segments = armId.TrimStart('/').Split('/');
        return segments.Length >= 4 ? segments[3] : armId;
    }
}
