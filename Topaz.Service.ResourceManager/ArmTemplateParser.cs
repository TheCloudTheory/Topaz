using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Templates.Engines;

namespace Topaz.Service.ResourceManager;

internal sealed class ArmTemplateParser
{
    public Template Parse(string input)
    {
        var template = TemplateParsingEngine.ParseTemplate(input);
        return template;
    }
}