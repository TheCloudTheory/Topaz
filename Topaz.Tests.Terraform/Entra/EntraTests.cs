namespace Topaz.Tests.Terraform.Entra;

/// <summary>
/// Tests the azuread Terraform provider against Topaz's emulated Entra (Azure AD) service.
/// The azuread provider communicates via Microsoft Graph — all Graph endpoints are served
/// by Topaz at topaz.local.dev:8899.
/// </summary>
public class EntraTests : TopazFixture
{
    [Test]
    public async Task Group_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureAd("group", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["group_display_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-test-group"));
                Assert.That(outputs["group_object_id"]!["value"]!.GetValue<string>(), Is.Not.Empty);
            });
        });
    }

    [Test]
    public async Task User_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureAd("user", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    outputs["user_upn"]!["value"]!.GetValue<string>(),
                    Is.EqualTo("tf-test-user@mytenant.onmicrosoft.com"));
                Assert.That(outputs["user_object_id"]!["value"]!.GetValue<string>(), Is.Not.Empty);
            });
        });
    }

    [Test]
    public async Task Application_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureAd("application", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["app_display_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-test-app"));
                Assert.That(outputs["app_client_id"]!["value"]!.GetValue<string>(), Is.Not.Empty);
            });
        });
    }

    [Test]
    public async Task ServicePrincipal_CreateAndDestroy_Succeeds()
    {
        await RunTerraformWithAzureAd("service_principal", outputs =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(outputs["sp_display_name"]!["value"]!.GetValue<string>(), Is.EqualTo("tf-test-sp-app"));
                Assert.That(outputs["sp_object_id"]!["value"]!.GetValue<string>(), Is.Not.Empty);
            });
        });
    }
}
