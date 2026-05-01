using Topaz.Portal.Components.Shared;
using Topaz.Portal.Models.Locations;

namespace Topaz.Tests.Portal;

[TestFixture]
public class LocationDropdownTests : BunitTestContext
{
    [Test]
    public void LocationDropdown_Renders_CorrectlyWithAllFeatures()
    {
        // Arrange & Act
        var cut = RenderComponent<LocationDropdown>();

        // Assert - Verify select element structure
        var select = cut.Find("select.form-select");
        Assert.That(select, Is.Not.Null);

        // Assert - All locations are rendered with placeholder
        var options = cut.FindAll("option");
        var expectedOptionCount = AzureLocations.CommonLocations.Count + 1;
        Assert.That(options.Count, Is.EqualTo(expectedOptionCount),
            $"Expected {expectedOptionCount} options but got {options.Count}");

        // Assert - Placeholder exists and is first
        var placeholderOption = options.First();
        Assert.That(placeholderOption.TextContent, Does.Contain("Select a location"));
        Assert.That(placeholderOption.GetAttribute("value"), Is.EqualTo(""));

        // Assert - All locations are present with correct values and display names
        for (int i = 0; i < AzureLocations.CommonLocations.Count; i++)
        {
            var expected = AzureLocations.CommonLocations[i];
            var actual = options[i + 1];
            
            Assert.That(actual.TextContent, Does.Contain(expected.DisplayName),
                $"Location {i}: Display name mismatch");
            Assert.That(actual.TextContent, Does.Contain(expected.Code),
                $"Location {i}: Location code mismatch");
            Assert.That(actual.GetAttribute("value"), Is.EqualTo(expected.Code),
                $"Location {i}: Value attribute mismatch");
        }

        // Assert - Bootstrap CSS class is applied
        var classList = select.GetAttribute("class");
        Assert.That(classList, Does.Contain("form-select"));
    }
}
