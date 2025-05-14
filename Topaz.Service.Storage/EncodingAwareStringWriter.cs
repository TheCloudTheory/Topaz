using System.Text;

namespace Topaz.Service.Storage;

/// <summary>
/// A <see cref="StringWriter"/> implementation that overrides the <see cref="Encoding"/> property
/// to always return UTF-8 encoding.
/// </summary>
internal sealed class EncodingAwareStringWriter : StringWriter
{
    public override Encoding Encoding { get; } = Encoding.UTF8;
}