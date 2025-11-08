#if NET8_0_OR_GREATER
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace MoonSharp.Benchmarking;

internal static class PerformanceReportWriter
{
    public static void Write(string suiteName, IEnumerable<Summary> summaries)
    {
        if (summaries == null)
        {
            return;
        }

        var summaryList = summaries.Where(s => s != null).ToList();
        if (summaryList.Count == 0)
        {
            return;
        }

        string osName = GetOsSectionName();
        string documentPath = LocatePerformanceDocument();
        string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");

        var firstSummary = summaryList[0];
        var env = firstSummary.HostEnvironmentInfo;

        var builder = new StringBuilder();
        builder.AppendLine($"## {osName}");
        builder.AppendLine();
        builder.AppendLine($"_Last updated: {timestamp}_");
        builder.AppendLine();
        builder.AppendLine("**Environment**");
        builder.AppendLine($"- OS: {RuntimeInformation.OSDescription}");

        var cpuInfo = env.CpuInfo;
        if (!string.IsNullOrWhiteSpace(cpuInfo.Value?.ProcessorName))
        {
            builder.AppendLine($"- CPU: {cpuInfo.Value.ProcessorName}");
        }

        builder.AppendLine($"- Logical cores: {Environment.ProcessorCount}");
        builder.AppendLine($"- Runtime: {env.RuntimeVersion}");

        var totalMemory = GetTotalSystemMemoryInMegabytes();
        if (totalMemory > 0)
        {
            builder.AppendLine($"- Approx. RAM: {totalMemory:N0} MB");
        }

        builder.AppendLine($"- Suite: {suiteName}");
        builder.AppendLine();

        foreach (var summary in summaryList)
        {
            var logger = new AccumulationLogger();
            MarkdownExporter.GitHub.ExportToLog(summary, logger);

            builder.AppendLine($"### {summary.Title}");
            builder.AppendLine();
            builder.AppendLine(logger.GetLog());
            builder.AppendLine();
        }

        ReplaceSection(documentPath, osName, suiteName, builder.ToString());
    }

    private static string GetOsSectionName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macOS";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        return RuntimeInformation.OSDescription;
    }

    private static string LocatePerformanceDocument()
    {
        string? current = AppContext.BaseDirectory;

        while (!string.IsNullOrWhiteSpace(current))
        {
            string candidate = Path.Combine(current, "docs", "Performance.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = Directory.GetParent(current)?.FullName;
        }

        throw new FileNotFoundException(
            "Unable to locate docs/Performance.md. Run benchmarks from within the repository."
        );
    }

    private static void ReplaceSection(
        string path,
        string osName,
        string suiteName,
        string replacement
    )
    {
        string content = File.ReadAllText(path);
        string header = $"## {osName}";
        string pattern = @$"{RegexEscape(header)}.*?(?=(\n##\s)|\Z)";
        var regex = new System.Text.RegularExpressions.Regex(
            pattern,
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        string existingSection = regex.Match(content).Success
            ? regex.Match(content).Value
            : string.Empty;

        string otherSuites = ExtractOtherSuites(existingSection, suiteName);

        string finalSection = replacement.TrimEnd();
        if (!string.IsNullOrWhiteSpace(otherSuites))
        {
            finalSection += Environment.NewLine + Environment.NewLine + otherSuites.Trim();
        }

        finalSection = finalSection.TrimEnd() + Environment.NewLine + Environment.NewLine;

        if (regex.IsMatch(content))
        {
            content = regex.Replace(content, finalSection, 1);
        }
        else
        {
            content = content.TrimEnd() + Environment.NewLine + Environment.NewLine + finalSection;
        }

        File.WriteAllText(path, content);
    }

    private static string RegexEscape(string value) =>
        System.Text.RegularExpressions.Regex.Escape(value);

    private static string ExtractOtherSuites(string existingSection, string suiteName)
    {
        if (string.IsNullOrWhiteSpace(existingSection))
        {
            return string.Empty;
        }

        int suiteIndex = existingSection.IndexOf("\n### ");
        if (suiteIndex < 0)
        {
            return string.Empty;
        }

        string suites = existingSection.Substring(suiteIndex);
        suites = suites.Replace("_No benchmark data recorded yet._", string.Empty);

        string suitePattern = @$"\n### {RegexEscape(suiteName)}.*?(?=(\n### )|\Z)";
        suites = System.Text.RegularExpressions.Regex.Replace(
            suites,
            suitePattern,
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline
        );

        return suites.Trim();
    }

    private static double GetTotalSystemMemoryInMegabytes()
    {
        try
        {
            var info = GC.GetGCMemoryInfo();
            if (info.TotalAvailableMemoryBytes > 0)
            {
                return info.TotalAvailableMemoryBytes / (1024d * 1024d);
            }
        }
        catch
        {
            // ignore – best-effort metric
        }

        return 0;
    }
}
#endif
