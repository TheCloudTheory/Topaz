using Topaz.Service.Entra.Endpoints.Applications;
using Topaz.Service.Entra.Endpoints.ServicePrincipal;
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
    public string Name => "Entra ID";

    public IReadOnlyCollection<IEndpointDefinition> Endpoints =>
    [
        new MeEndpoint(logger),
        new GetUserEndpoint(logger),
        new CreateUserEndpoint(logger),
        new DeleteUserEndpoint(logger),
        new ListServicePrincipalsEndpoint(logger),
        new GetServicePrincipalEndpoint(logger),
        new CreateServicePrincipalEndpoint(logger),
        new DeleteServicePrincipalEndpoint(logger),
        new UpdateServicePrincipalEndpoint(logger),
        new ListApplicationsEndpoint(logger),
    ];

    public void Bootstrap()
    {
        logger.LogDebug(nameof(EntraService), nameof(Bootstrap),$"Attempting to initialize Entra service directory directory...");
        
        var servicePath = Path.Combine(GlobalSettings.MainEmulatorDirectory, LocalDirectoryPath);
        var userPath = Path.Combine(servicePath, EntraResourceProvider.UsersDirectoryName);
        var servicePrincipalPath = Path.Combine(servicePath, EntraResourceProvider.ServicePrincipalsDirectoryName);
        var applicationsPath = Path.Combine(servicePath, EntraResourceProvider.ApplicationsDirectoryName);
        var userDataPath = Path.Combine(userPath, "data");
        var servicePrincipalDataPath = Path.Combine(servicePrincipalPath, "data");
        var applicationsDataPath = Path.Combine(applicationsPath, "data");
        
        CreateServiceDirectory(servicePath);
        CreateServiceDirectory(userPath);
        CreateServiceDirectory(userDataPath);
        CreateServiceDirectory(servicePrincipalPath);
        CreateServiceDirectory(servicePrincipalDataPath);
        CreateServiceDirectory(applicationsPath);
        CreateServiceDirectory(applicationsDataPath);
        
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
            UserPrincipalName = "topazadmin@topaz.local",
            PasswordProfile = new CreateUserRequest.PasswordProfileData
            {
                ForceChangePasswordNextSignIn = true,
                Password = "admin"
            }
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