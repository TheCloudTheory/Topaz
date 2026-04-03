using Microsoft.Graph;
using Microsoft.Graph.Models;
using Topaz.Portal.Models.Tenant;

namespace Topaz.Portal;

internal sealed partial class TopazClient
{
    public async Task<IReadOnlyList<User>> ListUsers(
        int top = 50,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        var resp = await _graphClient!.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = top;

            cfg.QueryParameters.Select =
            [
                "id",
                "displayName",
                "userPrincipalName",
                "mail",
                "accountEnabled",
                "userType"
            ];

            cfg.QueryParameters.Orderby = ["displayName"];
        }, cancellationToken);

        return resp?.Value ?? [];
    }

    public async Task CreateUser(
        string displayName,
        string userPrincipalName,
        string? mail,
        string password,
        bool accountEnabled = true,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();

        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));

        if (string.IsNullOrWhiteSpace(userPrincipalName))
            throw new ArgumentException("User principal name is required.", nameof(userPrincipalName));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var newUser = new User
        {
            DisplayName = displayName,
            UserPrincipalName = userPrincipalName,
            Mail = mail,
            AccountEnabled = accountEnabled,
            MailNickname = userPrincipalName.Split('@')[0],
            PasswordProfile = new PasswordProfile
            {
                Password = password,
                ForceChangePasswordNextSignIn = false
            }
        };

        await _graphClient!.Users.PostAsync(newUser, cancellationToken: cancellationToken);
    }

    public async Task<TenantInformationResponse> GetDirectoryInfo()
    {
        await EnsureInitializedAsync();

        var directory = await _graphClient!.Directory.GetAsync();
        var tenantInformation =
            await _graphClient!.TenantRelationships.FindTenantInformationByTenantIdWithTenantId(directory!.Id)
                .GetAsync();

        var users = await _graphClient!.Users.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var groups = await _graphClient!.Groups.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var servicePrincipals = await _graphClient!.ServicePrincipals.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        var applications = await _graphClient!.Applications.GetAsync(cfg =>
        {
            cfg.QueryParameters.Top = 1;
            cfg.QueryParameters.Count = true;
            cfg.Headers.Add("ConsistencyLevel", "eventual");
        });

        return TenantInformationResponse.FromGraph(
            tenantInformation!,
            users?.OdataCount,
            groups?.OdataCount,
            servicePrincipals?.OdataCount,
            applications?.OdataCount);
    }
}
