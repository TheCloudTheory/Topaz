namespace Topaz.Service.ApiManagement.Models;

internal sealed class PortalSignUpSettingsResourceProperties
{
    public bool Enabled { get; set; }
    public TermsOfServiceProperties? TermsOfService { get; set; }

    internal class TermsOfServiceProperties
    {
        public bool ConsentRequired { get; set; }
        public bool Enabled { get; set; }
        public string? Text { get; set; }
    }
}