using System.Xml.Linq;

namespace FleetVerification;

internal static class TestResults
{
    private static readonly string[] ExpectedProjects = ["FleetCompany.FleetManagement.Domain.Tests", "FleetCompany.FleetManagement.Application.Tests", "FleetCompany.FleetManagement.Integration.Tests"];
    public static void Verify(string directory)
    {
        var files = Directory.GetFiles(directory, "*.trx", SearchOption.AllDirectories);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var total = 0;
        foreach (var file in files)
        {
            var document = XDocument.Load(file);
            var ns = document.Root!.Name.Namespace;
            var counters = document.Descendants(ns + "Counters").Single();
            int Count(string name) => int.Parse(counters.Attribute(name)?.Value ?? throw new InvalidOperationException($"TRX lacks {name} counter."), System.Globalization.CultureInfo.InvariantCulture);
            var count = Count("total");
            if (count <= 0 || Count("executed") != count || Count("passed") != count || Count("failed") != 0 || Count("notExecuted") != 0)
                throw new InvalidOperationException("Full verification requires every discovered test to execute and pass; skipped or incomplete results are rejected.");
            var definitions = document.Descendants(ns + "UnitTest").ToArray();
            var definitionIds = definitions.Select(test => (string?)test.Attribute("id")).ToArray();
            if (definitions.Length != count || definitionIds.Any(string.IsNullOrWhiteSpace) || definitionIds.Distinct(StringComparer.Ordinal).Count() != count)
                throw new InvalidOperationException("TRX test definitions must be complete and unique.");
            var results = document.Descendants(ns + "UnitTestResult").ToArray();
            var resultIds = results.Select(result => (string?)result.Attribute("testId")).ToArray();
            if (results.Length != count || results.Any(result => (string?)result.Attribute("outcome") != "Passed") || resultIds.Any(string.IsNullOrWhiteSpace) || resultIds.Distinct(StringComparer.Ordinal).Count() != count || resultIds.Any(id => !definitionIds.Contains(id, StringComparer.Ordinal)))
                throw new InvalidOperationException("TRX individual results must match the counters and definitions, be unique, and all be Passed.");
            var projects = ExpectedProjects.Where(project => definitions.All(test => ((string?)test.Attribute("storage"))?.EndsWith(project + ".dll", StringComparison.OrdinalIgnoreCase) == true)).ToArray();
            if (projects.Length != 1 || !seen.Add(projects[0]))
                throw new InvalidOperationException("TRX must identify one distinct expected test project per report.");
            total += count;
            Console.WriteLine($"PASS: {projects[0]}: {count} passed, 0 failed, 0 skipped.");
        }

        if (seen.Count != ExpectedProjects.Length)
            throw new InvalidOperationException("Missing results: Domain, Application and Integration must all run.");
        Console.WriteLine($"PASS: Full suite: {total} passed; all three test projects ran without skips.");
    }
}
