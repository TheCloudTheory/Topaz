using Topaz.Service.Entra.Endpoints;
using Topaz.Service.Entra.Endpoints.Applications;
using Topaz.Service.Entra.Endpoints.Directory;
using Topaz.Service.Entra.Endpoints.Groups;
using Topaz.Service.Entra.Endpoints.ServicePrincipal;
using Topaz.Service.Entra.Endpoints.TenantRelationships;
using Topaz.Service.Entra.Endpoints.User;
using Topaz.Service.Entra.Models.Requests;
using Topaz.Service.Entra.Planes;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Service.Entra;

public class EntraService(ITopazLogger logger) : IServiceDefinition 
{
    public static bool IsGlobalService => true;
    public static string LocalDirectoryPath => ".entra";
    public static IReadOnlyCollection<string>? Subresources => null;
    public static string UniqueName => "entra";
    
    /// <summary>
    /// A static identifier of the Entra ID tenant. As Topaz supports only one tenant, this value can't be changed..
    /// </summary>
    public static string TenantId => GlobalSettings.DefaultTenantId;
    
    public static string TenantDisplayName => "Topaz";
    public static string DefaultDomainName => "topaz.local.dev";
    public static string FederationBrandName => "Topaz";
    
    public string Name => "Entra ID";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new MeEndpoint(logger),
        new GetUserEndpoint(logger),
        new CreateUserEndpoint(logger),
        new DeleteUserEndpoint(logger),
        new UpdateUserEndpoint(logger),
        new ListUsersEndpoint(logger),
        new ListServicePrincipalsEndpoint(logger),
        new GetServicePrincipalEndpoint(logger),
        new CreateServicePrincipalEndpoint(logger),
        new DeleteServicePrincipalEndpoint(logger),
        new UpdateServicePrincipalEndpoint(logger),
        new RemoveServicePrincipalOwnerEndpoint(logger),
        new GetServicePrincipalOwnersEndpoint(logger),
        new ListApplicationsEndpoint(logger),
        new CreateApplicationEndpoint(logger),
        new UpdateApplicationEndpoint(logger),
        new DeleteApplicationEndpoint(logger),
        new GetApplicationEndpoint(logger),
        new AddApplicationPasswordEndpoint(logger),
        new GetApplicationOwnersEndpoint(logger),
        new RemoveApplicationOwnerEndpoint(logger),
        new GetDirectoryEndpoint(logger),
        new FindTenantInformationByTenantIdEndpoint(logger),
        new ListGroupsEndpoint(logger),
        new CreateGroupEndpoint(logger),
        new GetGroupEndpoint(logger),
        new GetGroupOwnersEndpoint(logger),
        new GetGroupMembersEndpoint(logger),
        new GetGroupMemberOfEndpoint(logger),
        new UpdateGroupEndpoint(logger),
        new DeleteGroupEndpoint(logger),
        new OidcEndpoint(),
        new AuthorizeEndpoint(logger),
        new TokenEndpoint(logger)
    ];

    public void Bootstrap()
    {
        logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Attempting to initialize Entra service directory directory...");
        
        var servicePath = Path.Combine(GlobalSettings.MainEmulatorDirectory, LocalDirectoryPath);
        var userPath = Path.Combine(servicePath, EntraResourceProvider.UsersDirectoryName);
        var servicePrincipalPath = Path.Combine(servicePath, EntraResourceProvider.ServicePrincipalsDirectoryName);
        var applicationsPath = Path.Combine(servicePath, EntraResourceProvider.ApplicationsDirectoryName);
        var groupsPath = Path.Combine(servicePath, EntraResourceProvider.GroupsDirectoryName);
        var userDataPath = Path.Combine(userPath, "data");
        var servicePrincipalDataPath = Path.Combine(servicePrincipalPath, "data");
        var applicationsDataPath = Path.Combine(applicationsPath, "data");
        var groupsDataPath = Path.Combine(groupsPath, "data");
        
        CreateServiceDirectory(servicePath);
        CreateServiceDirectory(userPath);
        CreateServiceDirectory(userDataPath);
        CreateServiceDirectory(servicePrincipalPath);
        CreateServiceDirectory(servicePrincipalDataPath);
        CreateServiceDirectory(applicationsPath);
        CreateServiceDirectory(applicationsDataPath);
        CreateServiceDirectory(groupsPath);
        CreateServiceDirectory(groupsDataPath);
        
        logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Entra service directory directory initialized.");
        
        CreateSuperAdminUser();
    }

    private void CreateSuperAdminUser()
    {
        logger.LogDebug(nameof(EntraService), nameof(CreateSuperAdminUser),$"Creating super admin user.");
        
        var dataPlane = UserDataPlane.New(logger);
        _ = dataPlane.CreateSuperadmin(new CreateUserRequest
        {
            DisplayName = "Topaz Admin",
            MailNickname = "topazadmin",
            UserPrincipalName = "topazadmin@topaz.local.dev",
            PasswordProfile = new CreateUserRequest.PasswordProfileData
            {
                ForceChangePasswordNextSignIn = true,
                Password = "admin"
            },
            Mail = "topazadmin@topaz.local.dev"
        });
    }

    private void CreateServiceDirectory(string servicePath)
    {
        if(!Directory.Exists(servicePath))
        {
            Directory.CreateDirectory(servicePath);
            logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Directory {servicePath} created.");
        }
        else
        {
            logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Attempting to create {servicePath} directory - skipped.");
        }
    }
}