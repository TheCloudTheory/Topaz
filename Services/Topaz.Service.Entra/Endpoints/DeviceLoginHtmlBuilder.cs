using System.Web;

namespace Topaz.Service.Entra.Endpoints;

/// <summary>Builds the HTML pages served by <see cref="DeviceLoginEndpoint"/>.</summary>
internal static class DeviceLoginHtmlBuilder
{
    private const string BootstrapCdn =
        """<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous" />""";

    private const string TopazStyles = """
        <style>
            body { background: #f6f9ff; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #0f172a; }
            .btn-primary { background-color: #1d4ed8; border-color: #1d4ed8; }
            .btn-primary:hover { background-color: #2563eb; border-color: #2563eb; }
        </style>
        """;

    private const string LogoHtml = """
        <div class="text-center mb-3">
            <img src="https://topaz.local.dev:8899/topaz-logo.png"
                 alt="Topaz"
                 style="max-height: 128px; width: auto;"
                 onerror="this.style.display='none'" />
        </div>
        """;

    public static string Form(string? userCode, string? error)
    {
        var errorBlock = string.IsNullOrEmpty(error) ? "" : Alert("danger", error!);
        var userCodeValue = HttpUtility.HtmlEncode(userCode ?? "");

        var body = $"""
            {LogoHtml}
            <h3 class="card-title mb-3">Sign in to your device</h3>
            {errorBlock}
            <form method="POST" action="/devicelogin">
                <div class="mb-3">
                    <label class="form-label" for="user_code">Device Code</label>
                    <input id="user_code" name="user_code" class="form-control"
                           value="{userCodeValue}" autocomplete="off" />
                </div>
                <div class="mb-3">
                    <label class="form-label" for="username">Username</label>
                    <input id="username" name="username" class="form-control"
                           autocomplete="username" />
                </div>
                <button class="btn btn-primary" type="submit">Sign in</button>
            </form>
            """;

        return Layout("Sign in — Topaz", body);
    }

    public static string Success(string username)
    {
        var body = $"""
            {LogoHtml}
            <h3 class="card-title mb-3">You are signed in</h3>
            {Alert("success", $"Signed in as <strong>{HttpUtility.HtmlEncode(username)}</strong>. You can close this window and return to your application.")}
            """;

        return Layout("Signed in — Topaz", body);
    }

    private static string Alert(string type, string content) => $"""
        <div class="alert alert-{type}" role="alert">
            {content}
        </div>
        """;

    private static string Layout(string title, string cardBody) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>{HttpUtility.HtmlEncode(title)}</title>
            {BootstrapCdn}
            {TopazStyles}
        </head>
        <body>
        <div class="container" style="max-width: 520px; margin-top: 64px;">
            <div class="card">
                <div class="card-body">
                    {cardBody}
                </div>
            </div>
        </div>
        </body>
        </html>
        """;
}
