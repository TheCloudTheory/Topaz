namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class KeyVaultCertificateTests : PowerShellTestBase
{
    [Test]
    public async Task KeyVaultCertificateTests_WhenAddCommandIsCalled_CertificateShouldBeCreated()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-cert-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzKeyVault -Name PsCertVault01 -ResourceGroupName ps-kv-cert-rg -Location westeurope | Out-Null\n" +
            "$policy = New-AzKeyVaultCertificatePolicy -SubjectName 'CN=ps-test-cert' -IssuerName 'Self' -ValidityInMonths 12\n" +
            "Add-AzKeyVaultCertificate -VaultName PsCertVault01 -Name 'ps-test-cert' -CertificatePolicy $policy | Out-Null\n" +
            "$result = Get-AzKeyVaultCertificate -VaultName PsCertVault01 -Name 'ps-test-cert' | ConvertTo-Json -Depth 5\n" +
            "Remove-AzKeyVaultCertificate -VaultName PsCertVault01 -Name 'ps-test-cert' -Force | Out-Null\n" +
            "Remove-AzKeyVault -VaultName PsCertVault01 -ResourceGroupName ps-kv-cert-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-cert-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-test-cert"));
                    Assert.That(response["VaultName"]!.GetValue<string>(), Is.EqualTo("PsCertVault01").IgnoreCase);
                    Assert.That(response["Enabled"]!.GetValue<bool>(), Is.True);
                });
            });
    }

    [Test]
    public async Task KeyVaultCertificateTests_WhenGetCommandIsCalled_CertificateShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-cert-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzKeyVault -Name PsCertGetVault02 -ResourceGroupName ps-kv-cert-get-rg -Location westeurope | Out-Null\n" +
            "$policy = New-AzKeyVaultCertificatePolicy -SubjectName 'CN=ps-get-cert' -IssuerName 'Self' -ValidityInMonths 12\n" +
            "Add-AzKeyVaultCertificate -VaultName PsCertGetVault02 -Name 'ps-get-cert' -CertificatePolicy $policy | Out-Null\n" +
            "$result = Get-AzKeyVaultCertificate -VaultName PsCertGetVault02 -Name 'ps-get-cert' | ConvertTo-Json -Depth 5\n" +
            "Remove-AzKeyVaultCertificate -VaultName PsCertGetVault02 -Name 'ps-get-cert' -Force | Out-Null\n" +
            "Remove-AzKeyVault -VaultName PsCertGetVault02 -ResourceGroupName ps-kv-cert-get-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-cert-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("ps-get-cert"));
            });
    }

    [Test]
    public async Task KeyVaultCertificateTests_WhenListCommandIsCalled_AllCertificatesShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-cert-list-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzKeyVault -Name PsCertListVault03 -ResourceGroupName ps-kv-cert-list-rg -Location westeurope | Out-Null\n" +
            "$policyA = New-AzKeyVaultCertificatePolicy -SubjectName 'CN=ps-list-cert-a' -IssuerName 'Self' -ValidityInMonths 12\n" +
            "Add-AzKeyVaultCertificate -VaultName PsCertListVault03 -Name 'ps-list-cert-a' -CertificatePolicy $policyA | Out-Null\n" +
            "$policyB = New-AzKeyVaultCertificatePolicy -SubjectName 'CN=ps-list-cert-b' -IssuerName 'Self' -ValidityInMonths 12\n" +
            "Add-AzKeyVaultCertificate -VaultName PsCertListVault03 -Name 'ps-list-cert-b' -CertificatePolicy $policyB | Out-Null\n" +
            "$result = Get-AzKeyVaultCertificate -VaultName PsCertListVault03 | ConvertTo-Json -Depth 5\n" +
            "Remove-AzKeyVaultCertificate -VaultName PsCertListVault03 -Name 'ps-list-cert-a' -Force | Out-Null\n" +
            "Remove-AzKeyVaultCertificate -VaultName PsCertListVault03 -Name 'ps-list-cert-b' -Force | Out-Null\n" +
            "Remove-AzKeyVault -VaultName PsCertListVault03 -ResourceGroupName ps-kv-cert-list-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-cert-list-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                var array = response.AsArray();
                var certNames = array!
                    .Select(n => n!["Name"]!.GetValue<string>())
                    .ToList();

                Assert.That(certNames, Does.Contain("ps-list-cert-a"));
                Assert.That(certNames, Does.Contain("ps-list-cert-b"));
            });
    }

    [Test]
    public async Task KeyVaultCertificateTests_WhenDeleteCommandIsCalled_CertificateShouldNoLongerBeInList()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-kv-cert-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzKeyVault -Name PsCertDelVault04 -ResourceGroupName ps-kv-cert-del-rg -Location westeurope | Out-Null\n" +
            "$policy = New-AzKeyVaultCertificatePolicy -SubjectName 'CN=ps-del-cert' -IssuerName 'Self' -ValidityInMonths 12\n" +
            "Add-AzKeyVaultCertificate -VaultName PsCertDelVault04 -Name 'ps-del-cert' -CertificatePolicy $policy | Out-Null\n" +
            "Remove-AzKeyVaultCertificate -VaultName PsCertDelVault04 -Name 'ps-del-cert' -Force | Out-Null\n" +
            "$certs = Get-AzKeyVaultCertificate -VaultName PsCertDelVault04\n" +
            "$result = @{ Count = if ($null -eq $certs) { 0 } else { @($certs).Count } } | ConvertTo-Json\n" +
            "Remove-AzKeyVault -VaultName PsCertDelVault04 -ResourceGroupName ps-kv-cert-del-rg -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-kv-cert-del-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Count"]!.GetValue<int>(), Is.EqualTo(0));
            });
    }
}
