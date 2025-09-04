namespace Topaz.Host;

internal sealed class DnsManager
{
    private const string PlaceholderEntry = "# --- Topaz Host - DO NOT MODIFY ---";
    public void ConfigureEntries()
    {
        var filePath = GetHostsFilePath();
        var hosts = File.ReadAllLines(filePath).ToList();

        PrepareTopazPlaceholderForEntries(hosts);
        AddTopazEntries(filePath, hosts);
    }

    private void AddTopazEntries(string filePath, List<string> hosts)
    {
        BackupHostsFile(filePath);
        File.WriteAllLines(filePath, hosts);
    }

    private static void BackupHostsFile(string filePath)
    {
        var backupFilePath = $"{filePath}.backup";
        if (File.Exists(backupFilePath))
        {
            File.Delete(backupFilePath);
        }
        
        File.Copy(filePath, $"{filePath}.backup");
    }

    private static void PrepareTopazPlaceholderForEntries(List<string> hosts)
    {
        if (hosts.Any(host => host == PlaceholderEntry)) return;
        
        hosts.Add(Environment.NewLine);
        hosts.Add(PlaceholderEntry);
    }

    private static string GetHostsFilePath()
    {
        return Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => @"C:\Windows\System32\drivers\etc\hosts",
            PlatformID.Unix => "/etc/hosts",
            PlatformID.MacOSX => "/etc/hosts",
            _ => throw new PlatformNotSupportedException()
        };
    }
}