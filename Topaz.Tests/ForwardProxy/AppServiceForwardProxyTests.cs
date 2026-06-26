using Topaz.ForwardProxy;
using Topaz.Service.Shared;
using Topaz.Shared;

namespace Topaz.Tests.ForwardProxy;

public class AppServiceForwardProxyTests
{
    private AppServiceForwardProxy _proxy = null!;

    [SetUp]
    public void SetUp()
    {
        _proxy = new AppServiceForwardProxy(new HttpClient(), new PrettyTopazLogger());
    }

    [Test]
    public void CanForward_ValidAppServiceHost_ReturnsTrue()
    {
        Assert.That(_proxy.CanForward("myapp.azurewebsites.topaz.local.dev"), Is.True);
    }

    [Test]
    public void CanForward_AnotherValidAppServiceHost_ReturnsTrue()
    {
        Assert.That(_proxy.CanForward("some-other-app.azurewebsites.topaz.local.dev"), Is.True);
    }

    [Test]
    public void CanForward_OtherTopazHost_ReturnsFalse()
    {
        Assert.That(_proxy.CanForward("myvault.vault.topaz.local.dev"), Is.False);
    }

    [Test]
    public void CanForward_BareTopazHost_ReturnsFalse()
    {
        Assert.That(_proxy.CanForward("topaz.local.dev"), Is.False);
    }

    [Test]
    public void CanForward_EmptyString_ReturnsFalse()
    {
        Assert.That(_proxy.CanForward(string.Empty), Is.False);
    }

    [Test]
    public void CanForward_SuffixOnlyWithoutSubdomain_ReturnsFalse()
    {
        Assert.That(_proxy.CanForward("azurewebsites.topaz.local.dev"), Is.False);
    }
}
