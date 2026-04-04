namespace Topaz.Tests.Portal;

/// <summary>
/// Base class that owns the bUnit <see cref="Bunit.TestContext"/> and disposes it after each test.
/// Inherit from this instead of <see cref="Bunit.TestContext"/> directly so that NUnit lifecycle
/// methods are correctly honoured.
/// </summary>
public abstract class BunitTestContext : Bunit.TestContext
{
    [TearDown]
    public void TearDown() => Dispose();
}
