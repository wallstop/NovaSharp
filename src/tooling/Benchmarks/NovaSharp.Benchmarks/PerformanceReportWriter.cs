#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Text;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace NovaSharp.Benchmarking;

using System.Text.RegularExpressions;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Portability.Cpu;

internal static class PerformanceReportWriter
{
    public static void Write(string suiteName, IEnumerable<Summary> summaries)
    {
        if (summaries == null)
        {
            return;
        }

        List<Summary> summaryList = summaries.Where(s => s != null).ToList();
        if (summaryList.Count == 0)
        {
            return;
        }

        string osName = GetOsSectionName();
        string documentPath = LocatePerformanceDocument();
        string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz");

        Summary firstSummary = summaryList[0];
        string latestBlock = BuildLatestBlock(suiteName, timestamp, summaryList, firstSummary);

        ReplaceSection(documentPath, osName, latestBlock);
    }

    private static string GetOsSectionName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macOS";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }

        return RuntimeInformation.OSDescription;
    }

    private static string LocatePerformanceDocument()
    {
        string current = AppContext.BaseDirectory;

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

    private static string BuildLatestBlock(
        string suiteName,
        string timestamp,
        IReadOnlyList<Summary> summaries,
        Summary firstSummary
    )
    {
        HostEnvironmentInfo env = firstSummary.HostEnvironmentInfo;
        Lazy<CpuInfo> cpuInfo = env.CpuInfo;

        StringBuilder builder = new();

        builder.AppendLine($"### NovaSharp Latest (captured {timestamp})");
        builder.AppendLine();
        builder.AppendLine("**Environment**");
        builder.AppendLine($"- OS: {RuntimeInformation.OSDescription}");

        if (!string.IsNullOrWhiteSpace(cpuInfo.Value.ProcessorName))
        {
            builder.AppendLine($"- CPU: {cpuInfo.Value.ProcessorName}");
        }

        builder.AppendLine($"- Logical cores: {Environment.ProcessorCount}");
        builder.AppendLine($"- Runtime: {env.RuntimeVersion}");

        double totalMemory = GetTotalSystemMemoryInMegabytes();
        if (totalMemory > 0)
        {
            builder.AppendLine($"- Approx. RAM: {totalMemory:N0} MB");
        }

        builder.AppendLine($"- Suite: {suiteName}");
        builder.AppendLine();

        foreach (Summary summary in summaries)
        {
            AccumulationLogger logger = new();
            MarkdownExporter.GitHub.ExportToLog(summary, logger);

            builder.AppendLine($"#### {summary.Title}");
            builder.AppendLine();
            builder.AppendLine(logger.GetLog());
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static void ReplaceSection(string path, string osName, string latestBlock)
    {
        string content = File.ReadAllText(path);
        string header = $"## {osName}";
        string pattern = @$"{RegexEscape(header)}.*?(?=(\n##\s)|\Z)";
        Regex regex = new(pattern, RegexOptions.Singleline);

        Match match = regex.Match(content);
        string existingSection = match.Success ? match.Value : string.Empty;

        string baselineBlock = ExtractBaseline(existingSection);
        if (string.IsNullOrWhiteSpace(baselineBlock))
        {
            baselineBlock = "_No MoonSharp baseline recorded yet._";
        }

        StringBuilder sectionBuilder = new();
        sectionBuilder.AppendLine(header);
        sectionBuilder.AppendLine();
        sectionBuilder.AppendLine(baselineBlock.TrimEnd());
        sectionBuilder.AppendLine();
        sectionBuilder.AppendLine("---");
        sectionBuilder.AppendLine();
        sectionBuilder.AppendLine(latestBlock.TrimEnd());
        sectionBuilder.AppendLine();
        sectionBuilder.AppendLine("To refresh this section, run:");
        sectionBuilder.AppendLine();
        sectionBuilder.AppendLine("```");
        sectionBuilder.AppendLine(
            "dotnet run -c Release --project src/tooling/Benchmarks/NovaSharp.Benchmarks/NovaSharp.Benchmarks.csproj"
        );
        sectionBuilder.AppendLine(
            "dotnet run -c Release --framework net8.0 --project src/tooling/NovaSharp.Comparison/NovaSharp.Comparison.csproj"
        );
        sectionBuilder.AppendLine("```");
        sectionBuilder.AppendLine();
        sectionBuilder.AppendLine(
            "Then replace everything under `### NovaSharp Latest` with the new results."
        );
        sectionBuilder.AppendLine();

        string finalSection = sectionBuilder.ToString();

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

    private static string RegexEscape(string value) => Regex.Escape(value);

    private static string ExtractBaseline(string existingSection)
    {
        if (string.IsNullOrWhiteSpace(existingSection))
        {
            return string.Empty;
        }

        int baselineStart = existingSection.IndexOf("### MoonSharp", StringComparison.Ordinal);
        if (baselineStart < 0)
        {
            return string.Empty;
        }

        int separatorIndex = existingSection.IndexOf(
            "\n---",
            baselineStart,
            StringComparison.Ordinal
        );
        if (separatorIndex < 0)
        {
            separatorIndex = existingSection.IndexOf(
                "\n### NovaSharp",
                baselineStart,
                StringComparison.Ordinal
            );
        }

        int baselineEnd = separatorIndex >= 0 ? separatorIndex : existingSection.Length;
        string baseline = existingSection.Substring(baselineStart, baselineEnd - baselineStart);
        return baseline.Trim();
    }

    private static double GetTotalSystemMemoryInMegabytes()
    {
        try
        {
            GCMemoryInfo info = GC.GetGCMemoryInfo();
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
