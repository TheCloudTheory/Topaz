using JetBrains.Annotations;

namespace Topaz.Chaos;

[UsedImplicitly]
public sealed class ChaosStateProvider
{
    public static bool IsEnabled { get; internal set; }
}