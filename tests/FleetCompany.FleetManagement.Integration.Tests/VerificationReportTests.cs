using System.Xml.Linq;
using FleetVerification;
using Xunit;

namespace FleetCompany.FleetManagement.Integration.Tests;

public sealed class VerificationReportTests
{
    private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
    private static readonly string[] Projects = ["FleetCompany.FleetManagement.Domain.Tests", "FleetCompany.FleetManagement.Application.Tests", "FleetCompany.FleetManagement.Integration.Tests"];
    [Fact]
    public void Complete_reports_with_all_passed_individual_results_are_accepted()
    {
        using var reports = new Reports();
        TestResults.Verify(reports.Path);
    }

    [Theory]
    [InlineData("missing-result")]
    [InlineData("skipped-result")]
    [InlineData("failed-result")]
    [InlineData("duplicate-result")]
    [InlineData("unknown-test")]
    [InlineData("missing-definition")]
    [InlineData("duplicate-definition")]
    [InlineData("mixed-assemblies")]
    [InlineData("counter-mismatch")]
    [InlineData("missing-project")]
    [InlineData("duplicate-project")]
    public void Corrupt_or_incomplete_reports_are_rejected_even_if_summary_says_passed(string defect)
    {
        using var reports = new Reports();
        var file = System.IO.Path.Combine(reports.Path, "0.trx");
        var document = XDocument.Load(file);
        var results = document.Descendants(Ns + "UnitTestResult").ToArray();
        var definitions = document.Descendants(Ns + "UnitTest").ToArray();
        switch (defect)
        {
            case "missing-result":
                results[0].Remove();
                break;
            case "skipped-result":
                results[0].SetAttributeValue("outcome", "NotExecuted");
                break;
            case "failed-result":
                results[0].SetAttributeValue("outcome", "Failed");
                break;
            case "duplicate-result":
                results[1].SetAttributeValue("testId", (string?)results[0].Attribute("testId"));
                break;
            case "unknown-test":
                results[0].SetAttributeValue("testId", Guid.NewGuid());
                break;
            case "missing-definition":
                definitions[0].Remove();
                break;
            case "duplicate-definition":
                definitions[1].SetAttributeValue("id", (string?)definitions[0].Attribute("id"));
                break;
            case "mixed-assemblies":
                definitions[1].SetAttributeValue("storage", "Unrelated.Tests.dll");
                break;
            case "counter-mismatch":
                document.Descendants(Ns + "Counters").Single().SetAttributeValue("executed", 1);
                break;
            case "missing-project":
                File.Delete(System.IO.Path.Combine(reports.Path, "2.trx"));
                break;
            case "duplicate-project":
                File.Copy(file, System.IO.Path.Combine(reports.Path, "duplicate.trx"));
                break;
            default:
                throw new ArgumentException("Unknown defect.");
        }

        document.Save(file);
        Assert.Throws<InvalidOperationException>(() => TestResults.Verify(reports.Path));
    }

    private sealed class Reports : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fleet-trx-" + Guid.NewGuid().ToString("N"));

        public Reports()
        {
            Directory.CreateDirectory(Path);
            for (var i = 0; i < Projects.Length; i++)
            {
                var ids = new[]
                {
                    Guid.NewGuid().ToString(),
                    Guid.NewGuid().ToString()
                };
                new XDocument(new XElement(Ns + "TestRun", new XElement(Ns + "TestDefinitions", ids.Select(id => new XElement(Ns + "UnitTest", new XAttribute("id", id), new XAttribute("storage", Projects[i] + ".dll")))), new XElement(Ns + "Results", ids.Select(id => new XElement(Ns + "UnitTestResult", new XAttribute("testId", id), new XAttribute("outcome", "Passed")))), new XElement(Ns + "ResultSummary", new XElement(Ns + "Counters", new XAttribute("total", 2), new XAttribute("executed", 2), new XAttribute("passed", 2), new XAttribute("failed", 0), new XAttribute("notExecuted", 0))))).Save(System.IO.Path.Combine(Path, i + ".trx"));
            }
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
