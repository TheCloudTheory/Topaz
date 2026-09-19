using NUnit.Framework;
using Topaz.Service.Insights.Kql;
using Topaz.Service.Insights.Models;

namespace Topaz.Tests.Unit.Insights;

/// <summary>
/// Covers the app() function, which lets a query read telemetry from another Application Insights
/// component. The referenced component lives in its own resource group and subscription, so the
/// executor never resolves it itself: it forwards the reference to the dedicated loader.
/// </summary>
[TestFixture]
public class KqlQueryExecutorAppFunctionTests
{
    private const string CurrentComponent = "current-component";
    private const string ReferencedComponent = "referenced-component";
    private const string ReferencedResourceId =
        "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/referenced-rg/providers/Microsoft.Insights/components/referenced-component";
    private const string WorkspaceId = "11111111-1111-1111-1111-111111111111";

    private static string Request(string name) => $$"""{"name":"{{name}}"}""";

    private static IEnumerable<string> UnexpectedTableLoader(string workspaceName, string tableName) =>
        throw new InvalidOperationException(
            $"The workspace table loader must not be used for app(): {workspaceName}/{tableName}");

    private static IEnumerable<string> UnexpectedAppLoader(string appReference, string tableName) =>
        throw new InvalidOperationException(
            $"The app() loader must not be used here: {appReference}/{tableName}");

    private static string IdentityResolver(string reference) => reference;

    /// <summary>
    /// The executor returns rows as CLR values, so the first column of every row is projected to a
    /// string to keep the assertions independent of the JSON node types it hands out.
    /// </summary>
    private static string?[] FirstColumn(QueryResultTable table) =>
        [.. table.Rows.Select(row => row[0]?.ToString())];

    [Test]
    public void Execute_AppFunctionByName_LoadsTheReferencedComponent()
    {
        var calls = new List<(string Reference, string Table)>();
        IEnumerable<string> AppLoader(string appReference, string tableName)
        {
            calls.Add((appReference, tableName));
            return [Request("from-referenced-component")];
        }

        var result = KqlQueryExecutor.Execute(
            $"app(\"{ReferencedComponent}\").requests | take 10",
            CurrentComponent,
            UnexpectedTableLoader,
            IdentityResolver,
            AppLoader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls.Select(call => call.Reference), Is.EqualTo(new[] {ReferencedComponent}));
            Assert.That(calls.Select(call => call.Table), Is.EqualTo(new[] {"requests"}));
            Assert.That(result.Tables, Has.Length.EqualTo(1));
            Assert.That(result.Tables[0].Name, Is.EqualTo("PrimaryResult"));
            Assert.That(result.Tables[0].Columns.Select(column => column.Name), Is.EqualTo(new[] {"name"}));
            Assert.That(FirstColumn(result.Tables[0]), Is.EqualTo(new[] {"from-referenced-component"}));
        }
    }

    [Test]
    public void Execute_AppFunctionByResourceId_PassesTheResourceIdToTheLoader()
    {
        var calls = new List<(string Reference, string Table)>();
        IEnumerable<string> AppLoader(string appReference, string tableName)
        {
            calls.Add((appReference, tableName));
            return [Request("from-resource-id")];
        }

        var result = KqlQueryExecutor.Execute(
            $"app(\"{ReferencedResourceId}\").requests",
            CurrentComponent,
            UnexpectedTableLoader,
            IdentityResolver,
            AppLoader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls.Select(call => call.Reference), Is.EqualTo(new[] {ReferencedResourceId}));
            Assert.That(calls.Select(call => call.Table), Is.EqualTo(new[] {"requests"}));
            Assert.That(FirstColumn(result.Tables[0]), Is.EqualTo(new[] {"from-resource-id"}));
        }
    }

    [Test]
    public void Execute_AppFunctionWithoutATable_ReturnsNoRows()
    {
        var calls = 0;
        IEnumerable<string> AppLoader(string appReference, string tableName)
        {
            calls++;
            return [Request("unexpected")];
        }

        var result = KqlQueryExecutor.Execute(
            $"app(\"{ReferencedComponent}\")",
            CurrentComponent,
            UnexpectedTableLoader,
            IdentityResolver,
            AppLoader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls, Is.Zero, "app() without a table cannot be resolved to telemetry");
            Assert.That(result.Tables[0].Rows, Is.Empty);
        }
    }

    [Test]
    public void Execute_AppFunctionWithMalformedReference_ReturnsNoRows()
    {
        var calls = 0;
        IEnumerable<string> AppLoader(string appReference, string tableName)
        {
            calls++;
            return [Request("unexpected")];
        }

        var result = KqlQueryExecutor.Execute(
            "app().requests",
            CurrentComponent,
            UnexpectedTableLoader,
            IdentityResolver,
            AppLoader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls, Is.Zero);
            Assert.That(result.Tables[0].Rows, Is.Empty);
        }
    }

    [Test]
    public void Execute_UnionWithAppFunction_CombinesBothComponents()
    {
        var calls = new List<(string Reference, string Table)>();
        IEnumerable<string> AppLoader(string appReference, string tableName)
        {
            calls.Add((appReference, tableName));
            return [Request("from-referenced-component")];
        }

        IEnumerable<string> TableLoader(string workspaceName, string tableName) =>
            [Request("from-current-component")];

        var result = KqlQueryExecutor.Execute(
            $"union requests, app(\"{ReferencedComponent}\").requests | take 100",
            CurrentComponent,
            TableLoader,
            IdentityResolver,
            AppLoader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls.Select(call => call.Reference), Is.EqualTo(new[] {ReferencedComponent}));
            Assert.That(FirstColumn(result.Tables[0]),
                Is.EqualTo(new[] {"from-current-component", "from-referenced-component"}));
        }
    }

    [Test]
    public void Execute_WithoutAnAppLoader_ReturnsNoRowsInsteadOfFailing()
    {
        var result = KqlQueryExecutor.Execute(
            $"app(\"{ReferencedComponent}\").requests",
            CurrentComponent,
            UnexpectedTableLoader,
            IdentityResolver);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Tables[0].Rows, Is.Empty);
            Assert.That(result.Tables[0].Columns, Is.Empty);
        }
    }

    [Test]
    public void Execute_WorkspaceFunction_IsStillLoadedThroughTheWorkspaceLoader()
    {
        var calls = new List<(string Workspace, string Table)>();
        IEnumerable<string> TableLoader(string workspaceName, string tableName)
        {
            calls.Add((workspaceName, tableName));
            return [Request("from-workspace")];
        }

        var result = KqlQueryExecutor.Execute(
            $"workspace(\"{WorkspaceId}\").requests",
            CurrentComponent,
            TableLoader,
            reference => reference == WorkspaceId ? "resolved-workspace" : reference,
            UnexpectedAppLoader);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(calls.Select(call => call.Workspace), Is.EqualTo(new[] {"resolved-workspace"}));
            Assert.That(calls.Select(call => call.Table), Is.EqualTo(new[] {"requests"}));
            Assert.That(FirstColumn(result.Tables[0]), Is.EqualTo(new[] {"from-workspace"}));
        }
    }
}
