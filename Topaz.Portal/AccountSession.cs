using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Topaz.Portal;

public sealed class AccountSession(ProtectedSessionStorage sessionStorage, TopazClient client)
{
    private const string SelectedAccountKey = "topaz.selected-account";
    private const string UsernameKey = "topaz.username";

    public string? SelectedAccount { get; private set; }
    private string? Username { get; set; }

    public async Task LoadAsync()
    {
        var selected = await sessionStorage.GetAsync<string>(SelectedAccountKey);
        SelectedAccount = selected.Success ? selected.Value : null;

        var user = await sessionStorage.GetAsync<string>(UsernameKey);
        Username = user.Success ? user.Value : null;
    }
    
    public async Task<(bool Ok, string? ErrorMessage)> SignInWithErrorAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "Username and password are required.");

        var (token, error) = await client.GetAuthTokenWithError(username, password);
        if (token is null)
            return (false, error ?? "Sign-in failed.");

        SelectedAccount = username.Trim();
        Username = username.Trim();

        await sessionStorage.SetAsync(SelectedAccountKey, SelectedAccount);
        await sessionStorage.SetAsync(UsernameKey, Username);

        return (true, null);
    }

    public async Task SignOutAsync()
    {
        SelectedAccount = null;
        Username = null;

        await sessionStorage.DeleteAsync(SelectedAccountKey);
        await sessionStorage.DeleteAsync(UsernameKey);
    }
}