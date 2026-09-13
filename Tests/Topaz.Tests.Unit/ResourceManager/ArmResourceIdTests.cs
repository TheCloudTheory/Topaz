using NUnit.Framework;
using Topaz.ResourceManager;

namespace Topaz.Tests.Unit.ResourceManager;

[TestFixture]
public class ArmResourceIdTests
{
    private const string ResourceGroupScope =
        "/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/rg";

    [Test]
    public void Build_PairsEachTypeSegmentWithItsNameSegment()
    {
        var id = ArmResourceId.Build(
            ResourceGroupScope, "Microsoft.ServiceBus/namespaces/topics/subscriptions/rules", "ns/topic/sub/rule");

        Assert.That(id, Is.EqualTo(
            $"{ResourceGroupScope}/providers/Microsoft.ServiceBus/namespaces/ns/topics/topic/subscriptions/sub/rules/rule"));
    }

    [Test]
    public void Build_LeavesATopLevelResourceUnchanged()
    {
        var id = ArmResourceId.Build(ResourceGroupScope, "Microsoft.ServiceBus/namespaces", "ns");

        Assert.That(id, Is.EqualTo($"{ResourceGroupScope}/providers/Microsoft.ServiceBus/namespaces/ns"));
    }

    [Test]
    public void Build_HangsOffAnEmptyScopeAtTenantLevel()
    {
        var id = ArmResourceId.Build(string.Empty, "Microsoft.Management/managementGroups", "mg");

        Assert.That(id, Is.EqualTo("/providers/Microsoft.Management/managementGroups/mg"));
    }

    /// <summary>
    /// A name that does not pair with the type is left as it was rather than guessed at, so a
    /// malformed template produces a recognisably wrong id instead of a plausible one.
    /// </summary>
    [TestCase("Microsoft.ServiceBus/namespaces/topics", "topic")]
    [TestCase("Microsoft.ServiceBus/namespaces", "ns/topic")]
    public void Build_FallsBackWhenTypeAndNameDoNotPair(string type, string name)
    {
        var id = ArmResourceId.Build(ResourceGroupScope, type, name);

        Assert.That(id, Is.EqualTo($"{ResourceGroupScope}/providers/{type}/{name}"));
    }
}
