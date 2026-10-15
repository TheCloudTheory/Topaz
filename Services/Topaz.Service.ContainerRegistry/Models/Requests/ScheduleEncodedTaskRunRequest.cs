using System.Text;
using JetBrains.Annotations;
using Topaz.Service.Shared.Models;

namespace Topaz.Service.ContainerRegistry.Models.Requests;

[UsedImplicitly]
internal sealed class ScheduleEncodedTaskRunRequest : TopazApiRequest
{
    public string Type { get; init; } = string.Empty;
    public AcrTaskPlatformProperties? Platform { get; init; }
    public string? EncodedTaskContent { get; init; }

    /// <summary>
    /// Decodes the Base64-encoded task content stored in the <c>EncodedTaskContent</c> property
    /// and returns it as a UTF-8 string.
    /// </summary>
    /// <returns>
    /// The decoded UTF-8 string representation of the task content.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>EncodedTaskContent</c> property is null or empty.
    /// </exception>
    public string Decode()
    {
        return string.IsNullOrEmpty(EncodedTaskContent)
            ? throw new InvalidOperationException("Encoded task content is null or empty")
            : Encoding.UTF8.GetString(Convert.FromBase64String(EncodedTaskContent));
    }
}