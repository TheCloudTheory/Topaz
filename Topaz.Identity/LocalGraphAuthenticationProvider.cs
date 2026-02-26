using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Topaz.Identity;

public class LocalGraphAuthenticationProvider : IAuthenticationProvider
{
    public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = new())
    {
        request.Headers.Add("Authorization", "Bearer " + JwtHelper.GenerateJwt(Globals.GlobalAdminId, true));
        return Task.CompletedTask;
    }
}