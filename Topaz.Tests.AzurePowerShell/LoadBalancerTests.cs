namespace Topaz.Tests.AzurePowerShell;

[Parallelizable(ParallelScope.Fixtures)]
public class LoadBalancerTests : PowerShellTestBase
{
    [Test]
    public async Task LoadBalancerTests_WhenNewAzLoadBalancerCommandIsCalled_LoadBalancerShouldBeCreatedWithCorrectProperties()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-lb-create-rg -Location westeurope -Force | Out-Null\n" +
            "$result = New-AzLoadBalancer -ResourceGroupName ps-lb-create-rg -Name PsTestLb01 -Location westeurope -Sku Standard | ConvertTo-Json -Depth 10\n" +
            "Remove-AzLoadBalancer -ResourceGroupName ps-lb-create-rg -Name PsTestLb01 -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-lb-create-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsTestLb01"));
                    Assert.That(response["ResourceGroupName"]!.GetValue<string>(), Is.EqualTo("ps-lb-create-rg"));
                    Assert.That(response["Location"]!.GetValue<string>(), Is.EqualTo("westeurope").IgnoreCase);
                });
            });
    }

    [Test]
    public async Task LoadBalancerTests_WhenGetAzLoadBalancerCommandIsCalled_LoadBalancerShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-lb-get-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzLoadBalancer -ResourceGroupName ps-lb-get-rg -Name PsGetLb02 -Location westeurope -Sku Standard | Out-Null\n" +
            "$result = Get-AzLoadBalancer -ResourceGroupName ps-lb-get-rg -Name PsGetLb02 | ConvertTo-Json -Depth 10\n" +
            "Remove-AzLoadBalancer -ResourceGroupName ps-lb-get-rg -Name PsGetLb02 -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-lb-get-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                Assert.That(response["Name"]!.GetValue<string>(), Is.EqualTo("PsGetLb02"));
            });
    }

    [Test]
    public async Task LoadBalancerTests_WhenRemoveAzLoadBalancerCommandIsCalled_LoadBalancerShouldBeDeleted()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-lb-del-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzLoadBalancer -ResourceGroupName ps-lb-del-rg -Name PsDelLb03 -Location westeurope -Sku Standard | Out-Null\n" +
            "Remove-AzLoadBalancer -ResourceGroupName ps-lb-del-rg -Name PsDelLb03 -Force | Out-Null\n" +
            "$exists = (Get-AzLoadBalancer -ResourceGroupName ps-lb-del-rg -Name PsDelLb03 -ErrorAction SilentlyContinue) -ne $null\n" +
            "Remove-AzResourceGroup -Name ps-lb-del-rg -Force | Out-Null\n" +
            "ConvertTo-Json @{ exists = $exists }",
            response =>
            {
                Assert.That(response["exists"]!.GetValue<bool>(), Is.False);
            });
    }

    [Test]
    public async Task LoadBalancerTests_WhenLoadBalancerTagsAreUpdated_TagsShouldPersist()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-lb-tag-rg -Location westeurope -Force | Out-Null\n" +
            "$lb = New-AzLoadBalancer -ResourceGroupName ps-lb-tag-rg -Name PsTagLb04 -Location westeurope -Sku Standard\n" +
            "$lb.Tag = @{env='test'; team='platform'}\n" +
            "$result = Set-AzLoadBalancer -LoadBalancer $lb | ConvertTo-Json -Depth 10\n" +
            "Remove-AzLoadBalancer -ResourceGroupName ps-lb-tag-rg -Name PsTagLb04 -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-lb-tag-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                var tags = response["Tag"]!.AsObject();
                Assert.Multiple(() =>
                {
                    Assert.That(tags.ContainsKey("env"), Is.True);
                    Assert.That(tags["env"]!.GetValue<string>(), Is.EqualTo("test"));
                    Assert.That(tags["team"]!.GetValue<string>(), Is.EqualTo("platform"));
                });
            });
    }

    [Test]
    public async Task LoadBalancerTests_WhenGetAzLoadBalancerWithoutNameIsCalled_AllLoadBalancersShouldBeReturned()
    {
        await RunAzurePowerShellCommand(
            "New-AzResourceGroup -Name ps-lb-list-rg -Location westeurope -Force | Out-Null\n" +
            "New-AzLoadBalancer -ResourceGroupName ps-lb-list-rg -Name PsListLb05 -Location westeurope -Sku Standard | Out-Null\n" +
            "New-AzLoadBalancer -ResourceGroupName ps-lb-list-rg -Name PsListLb06 -Location westeurope -Sku Basic | Out-Null\n" +
            "$result = Get-AzLoadBalancer -ResourceGroupName ps-lb-list-rg | ConvertTo-Json -Depth 10\n" +
            "Remove-AzLoadBalancer -ResourceGroupName ps-lb-list-rg -Name PsListLb05 -Force | Out-Null\n" +
            "Remove-AzLoadBalancer -ResourceGroupName ps-lb-list-rg -Name PsListLb06 -Force | Out-Null\n" +
            "Remove-AzResourceGroup -Name ps-lb-list-rg -Force | Out-Null\n" +
            "$result",
            response =>
            {
                var names = response.AsArray()!.Select(lb => lb!["Name"]!.GetValue<string>()).ToList();
                Assert.Multiple(() =>
                {
                    Assert.That(names, Does.Contain("PsListLb05"));
                    Assert.That(names, Does.Contain("PsListLb06"));
                });
            });
    }
}
