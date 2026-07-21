namespace Topaz.Tests.Legacy.Java;

public class JavaTestRunner
{
    [Test]
    public async Task Java_BlobStorageLegacySdkTests()
    {
        await JavaHostMapper.EnsureStorageHostsMapped("wasbtestaccount");
        await JavaFixture.RunJavaTests("BlobStorageLegacySdkTest");
    }
}
