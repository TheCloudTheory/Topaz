using Topaz.Portal.Components.Shared;

namespace Topaz.Tests.Portal;

file static class TagsPanelHelpers
{
    public static Dictionary<string, string> SingleTag(string key = "env", string value = "prod")
        => new(StringComparer.OrdinalIgnoreCase) { { key, value } };

    public static Func<string, string, Task<string?>> SuccessAdd =>
        (_, _) => Task.FromResult<string?>(null);

    public static Func<string, Task<string?>> SuccessRemove =>
        _ => Task.FromResult<string?>(null);

    public static Func<string, string, Task<string?>> SuccessEdit =>
        (_, _) => Task.FromResult<string?>(null);
}

[TestFixture]
public class TagsPanel_AddTag_CallsOnAddWithCorrectArguments : BunitTestContext
{
    [Test]
    public void Test()
    {
        string? addedKey = null;
        string? addedValue = null;

        var cut = Render<TagsPanel>(p => p
            .Add(x => x.Tags, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase))
            .Add(x => x.OnAdd, (key, value) =>
            {
                addedKey = key;
                addedValue = value;
                return Task.FromResult<string?>(null);
            })
            .Add(x => x.OnRemove, TagsPanelHelpers.SuccessRemove)
            .Add(x => x.OnEdit, TagsPanelHelpers.SuccessEdit));

        cut.Find("#tagsPanelName").Input("environment");
        cut.Find("#tagsPanelValue").Input("production");
        cut.Find("button.btn-primary").Click();

        Assert.That(addedKey, Is.EqualTo("environment"));
        Assert.That(addedValue, Is.EqualTo("production"));
    }
}

[TestFixture]
public class TagsPanel_AddTag_DuplicateKey_ShowsError : BunitTestContext
{
    [Test]
    public void Test()
    {
        var cut = Render<TagsPanel>(p => p
            .Add(x => x.Tags, TagsPanelHelpers.SingleTag("env", "prod"))
            .Add(x => x.OnAdd, TagsPanelHelpers.SuccessAdd)
            .Add(x => x.OnRemove, TagsPanelHelpers.SuccessRemove)
            .Add(x => x.OnEdit, TagsPanelHelpers.SuccessEdit));

        cut.Find("#tagsPanelName").Input("env");
        cut.Find("#tagsPanelValue").Input("staging");
        cut.Find("button.btn-primary").Click();

        Assert.That(cut.Find(".alert-danger").TextContent, Does.Contain("env"));
    }
}

[TestFixture]
public class TagsPanel_EditTag_ClickEdit_ShowsInputPrefilledWithCurrentValue : BunitTestContext
{
    [Test]
    public void Test()
    {
        var cut = Render<TagsPanel>(p => p
            .Add(x => x.Tags, TagsPanelHelpers.SingleTag("env", "prod"))
            .Add(x => x.OnAdd, TagsPanelHelpers.SuccessAdd)
            .Add(x => x.OnRemove, TagsPanelHelpers.SuccessRemove)
            .Add(x => x.OnEdit, TagsPanelHelpers.SuccessEdit));

        cut.Find("button.btn-outline-primary").Click();

        var editInput = cut.Find("input.form-control-sm");
        Assert.That(editInput.GetAttribute("value"), Is.EqualTo("prod"));
        Assert.That(cut.FindAll("button").Any(b => b.TextContent.Trim() == "Save"), Is.True);
        Assert.That(cut.FindAll("button").Any(b => b.TextContent.Trim() == "Cancel"), Is.True);
    }
}

[TestFixture]
public class TagsPanel_EditTag_Save_CallsOnEditAndExitsEditMode : BunitTestContext
{
    [Test]
    public void Test()
    {
        string? editedKey = null;
        string? editedValue = null;

        var cut = Render<TagsPanel>(p => p
            .Add(x => x.Tags, TagsPanelHelpers.SingleTag("env", "prod"))
            .Add(x => x.OnAdd, TagsPanelHelpers.SuccessAdd)
            .Add(x => x.OnRemove, TagsPanelHelpers.SuccessRemove)
            .Add(x => x.OnEdit, (key, value) =>
            {
                editedKey = key;
                editedValue = value;
                return Task.FromResult<string?>(null);
            }));

        cut.Find("button.btn-outline-primary").Click();
        cut.Find("input.form-control-sm").Input("staging");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Save").Click();

        Assert.That(editedKey, Is.EqualTo("env"));
        Assert.That(editedValue, Is.EqualTo("staging"));
        Assert.That(cut.FindAll("input.form-control-sm"), Is.Empty);
    }
}

[TestFixture]
public class TagsPanel_EditTag_Cancel_RestoresDisplayMode : BunitTestContext
{
    [Test]
    public void Test()
    {
        var cut = Render<TagsPanel>(p => p
            .Add(x => x.Tags, TagsPanelHelpers.SingleTag("env", "prod"))
            .Add(x => x.OnAdd, TagsPanelHelpers.SuccessAdd)
            .Add(x => x.OnRemove, TagsPanelHelpers.SuccessRemove)
            .Add(x => x.OnEdit, TagsPanelHelpers.SuccessEdit));

        cut.Find("button.btn-outline-primary").Click();
        cut.Find("input.form-control-sm").Input("staging");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Cancel").Click();

        Assert.That(cut.FindAll("input.form-control-sm"), Is.Empty);
        Assert.That(cut.Find("td:nth-child(2)").TextContent.Trim(), Is.EqualTo("prod"));
    }
}

[TestFixture]
public class TagsPanel_RemoveTag_CallsOnRemoveWithCorrectKey : BunitTestContext
{
    [Test]
    public void Test()
    {
        string? removedKey = null;

        var cut = Render<TagsPanel>(p => p
            .Add(x => x.Tags, TagsPanelHelpers.SingleTag("env", "prod"))
            .Add(x => x.OnAdd, TagsPanelHelpers.SuccessAdd)
            .Add(x => x.OnRemove, key =>
            {
                removedKey = key;
                return Task.FromResult<string?>(null);
            })
            .Add(x => x.OnEdit, TagsPanelHelpers.SuccessEdit));

        cut.Find("button.btn-outline-danger").Click();

        Assert.That(removedKey, Is.EqualTo("env"));
    }
}
