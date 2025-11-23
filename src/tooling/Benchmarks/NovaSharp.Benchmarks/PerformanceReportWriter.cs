namespace NovaSharp.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using System.Text.RegularExpressions;
    using BenchmarkDotNet.Environments;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Loggers;
    using BenchmarkDotNet.Reports;
    using BenchmarkDotNet.Running;
    using Microsoft.Win32;
    using Perfolizer.Models;

#nullable enable

    internal static class PerformanceReportWriter
    {
        public static void Write(string suiteName, IEnumerable<Summary> summaries)
        {
            if (summaries == null)
            {
                return;
            }

            List<Summary> summaryList = summaries.Where(static s => s != null).ToList();
            if (summaryList.Count == 0)
            {
                return;
            }

            string osSectionName = GetOsSectionName();
            string friendlyOsDescription = GetFriendlyOsDescription();
            string documentPath = LocatePerformanceDocument();
            string documentContent = File.ReadAllText(documentPath);
            string existingSection = ExtractOsSection(documentContent, osSectionName);
            string baselineBlock = ExtractBaseline(existingSection);
            Dictionary<BenchmarkKey, BaselineMetrics> baselineMetrics = ParseBaselineMetrics(
                baselineBlock
            );

            IReadOnlyList<ComparisonRow> comparisons = BuildComparisonRows(
                summaryList,
                baselineMetrics
            );

            Summary firstSummary = summaryList[0];
            string timestamp = DateTimeOffset.Now.ToString(
                "yyyy-MM-dd HH:mm:ss zzz",
                CultureInfo.InvariantCulture
            );
            string latestBlock = BuildLatestBlock(
                suiteName,
                timestamp,
                summaryList,
                firstSummary,
                comparisons,
                friendlyOsDescription
            );

            string updatedContent = ReplaceSection(
                documentContent,
                osSectionName,
                baselineBlock,
                latestBlock
            );

            File.WriteAllText(documentPath, updatedContent);
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

        private static string GetFriendlyOsDescription()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RuntimeInformation.OSDescription;
            }

            try
            {
                using RegistryKey? currentVersion = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"
                );

                if (currentVersion != null)
                {
                    string? productName = currentVersion.GetValue("ProductName") as string;
                    string? displayVersion = currentVersion.GetValue("DisplayVersion") as string;
                    string? releaseId = currentVersion.GetValue("ReleaseId") as string;
                    string? currentBuild = currentVersion.GetValue("CurrentBuild") as string;
                    string? editionId = currentVersion.GetValue("EditionID") as string;
                    object? ubrValue = currentVersion.GetValue("UBR");

                    string? buildNumber = null;
                    if (!string.IsNullOrWhiteSpace(currentBuild))
                    {
                        string revision = ubrValue switch
                        {
                            int ubrInt => ubrInt.ToString(CultureInfo.InvariantCulture),
                            uint ubrUint => ubrUint.ToString(CultureInfo.InvariantCulture),
                            long ubrLong => ubrLong.ToString(CultureInfo.InvariantCulture),
                            string ubrString when !string.IsNullOrWhiteSpace(ubrString) =>
                                ubrString,
                            _ => string.Empty,
                        };

                        buildNumber = string.IsNullOrWhiteSpace(revision)
                            ? currentBuild
                            : $"{currentBuild}.{revision}";
                    }

                    string versionLabel =
                        !string.IsNullOrWhiteSpace(displayVersion) ? displayVersion
                        : !string.IsNullOrWhiteSpace(releaseId) ? releaseId
                        : string.Empty;

                    Version osVersion = Environment.OSVersion.Version;
                    string kernelVersion = $"{osVersion.Major}.{osVersion.Minor}.{osVersion.Build}";
                    string familyLabel = osVersion.Build >= 22000 ? "Windows 11" : "Windows 10";
                    string editionLabel = DeriveEditionLabel(productName, editionId);

                    StringBuilder description = new();
                    description.Append(familyLabel);

                    if (!string.IsNullOrWhiteSpace(editionLabel))
                    {
                        description.Append(' ');
                        description.Append(editionLabel);
                    }

                    if (!string.IsNullOrWhiteSpace(versionLabel))
                    {
                        description.Append(' ');
                        description.Append(versionLabel);
                    }

                    description.Append(" (build ");
                    description.Append(
                        string.IsNullOrWhiteSpace(buildNumber)
                            ? osVersion.Build.ToString(CultureInfo.InvariantCulture)
                            : buildNumber
                    );

                    if (!string.IsNullOrWhiteSpace(kernelVersion))
                    {
                        description.Append(", ");
                        description.Append(kernelVersion);
                    }

                    description.Append(')');

                    return description.ToString();
                }
            }
            catch (SecurityException)
            {
                // fall back to the version-based heuristic below
            }
            catch (UnauthorizedAccessException)
            {
                // fall back to the version-based heuristic below
            }
            catch (IOException)
            {
                // fall back to the version-based heuristic below
            }

            Version fallbackVersion = Environment.OSVersion.Version;
            bool isWindows11 = fallbackVersion.Build >= 22000;
            string familyName = isWindows11 ? "Windows 11" : "Windows 10";
            string buildInfo =
                $"{fallbackVersion.Major}.{fallbackVersion.Minor}.{fallbackVersion.Build}";

            return $"{familyName} (build {fallbackVersion.Build}, {buildInfo})";
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

                current = Directory.GetParent(current)?.FullName ?? string.Empty;
            }

            throw new FileNotFoundException(
                "Unable to locate docs/Performance.md. Run benchmarks from within the repository."
            );
        }

        private static string ExtractOsSection(string documentContent, string osSectionName)
        {
            if (string.IsNullOrWhiteSpace(documentContent))
            {
                return string.Empty;
            }

            string header = $"## {osSectionName}";
            string normalized = NormalizeLineEndings(documentContent);

            int headerIndex = normalized.IndexOf(header, StringComparison.Ordinal);
            if (headerIndex < 0)
            {
                return string.Empty;
            }

            int nextHeaderIndex = normalized.IndexOf(
                "\n## ",
                headerIndex + header.Length,
                StringComparison.Ordinal
            );
            if (nextHeaderIndex < 0)
            {
                nextHeaderIndex = normalized.Length;
            }

            return normalized.Substring(headerIndex, nextHeaderIndex - headerIndex).Trim();
        }

        private static void AppendLineInvariant(StringBuilder builder, FormattableString value) =>
            builder.AppendLine(FormattableString.Invariant(value));

        private static string BuildLatestBlock(
            string suiteName,
            string timestamp,
            IReadOnlyList<Summary> summaries,
            Summary firstSummary,
            IReadOnlyList<ComparisonRow> comparisons,
            string friendlyOsDescription
        )
        {
            HostEnvironmentInfo env = firstSummary.HostEnvironmentInfo;
            Lazy<CpuInfo> cpuInfo = env.Cpu;

            StringBuilder builder = new();

            AppendLineInvariant(builder, $"### NovaSharp Latest (captured {timestamp})");
            builder.AppendLine();
            builder.AppendLine("**Environment**");
            AppendLineInvariant(builder, $"- OS: {friendlyOsDescription}");

            if (!string.IsNullOrWhiteSpace(cpuInfo.Value.ProcessorName))
            {
                AppendLineInvariant(builder, $"- CPU: {cpuInfo.Value.ProcessorName}");
            }

            AppendLineInvariant(builder, $"- Logical cores: {Environment.ProcessorCount}");
            AppendLineInvariant(builder, $"- Runtime: {env.RuntimeVersion}");

            double totalMemory = GetTotalSystemMemoryInMegabytes();
            if (totalMemory > 0)
            {
                AppendLineInvariant(builder, $"- Approx. RAM: {totalMemory:N0} MB");
            }

            AppendLineInvariant(builder, $"- Suite: {suiteName}");
            builder.AppendLine();

            if (comparisons.Count > 0)
            {
                builder.AppendLine("**Delta vs MoonSharp baseline**");
                builder.AppendLine();
                builder.AppendLine(
                    "| Summary | Method | Parameters | Nova Mean | MoonSharp Mean | Mean Δ | Mean Δ % | Nova Alloc | MoonSharp Alloc | Alloc Δ | Alloc Δ % |"
                );
                builder.AppendLine(
                    "|-------- |------- |----------- |----------:|---------------:|-------:|--------:|-----------:|----------------:|-------:|----------:|"
                );

                foreach (ComparisonRow row in comparisons)
                {
                    builder.Append("| ");
                    builder.Append(row.SummaryName);
                    builder.Append(" | ");
                    builder.Append(row.Method);
                    builder.Append(" | ");
                    builder.Append(row.ParameterDisplay);
                    builder.Append(" | ");
                    builder.Append(FormatTime(row.NovaMeanNanoseconds));
                    builder.Append(" | ");
                    builder.Append(FormatTime(row.BaselineMeanNanoseconds));
                    builder.Append(" | ");
                    builder.Append(FormatTimeDifference(row.MeanDeltaNanoseconds));
                    builder.Append(" | ");
                    builder.Append(FormatPercentage(row.MeanDeltaPercent));
                    builder.Append(" | ");
                    builder.Append(FormatBytes(row.NovaAllocatedBytes));
                    builder.Append(" | ");
                    builder.Append(FormatBytes(row.BaselineAllocatedBytes));
                    builder.Append(" | ");
                    builder.Append(FormatBytesDifference(row.AllocatedDeltaBytes));
                    builder.Append(" | ");
                    builder.Append(FormatPercentage(row.AllocatedDeltaPercent));
                    builder.AppendLine(" |");
                }

                builder.AppendLine();
            }

            foreach (Summary summary in summaries)
            {
                AccumulationLogger logger = new();
                MarkdownExporter.GitHub.ExportToLog(summary, logger);

                AppendLineInvariant(builder, $"#### {summary.Title}");
                builder.AppendLine();
                builder.AppendLine(logger.GetLog());
                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }

        private static IReadOnlyList<ComparisonRow> BuildComparisonRows(
            IReadOnlyList<Summary> summaries,
            IReadOnlyDictionary<BenchmarkKey, BaselineMetrics> baselineMetrics
        )
        {
            if (baselineMetrics.Count == 0)
            {
                return Array.Empty<ComparisonRow>();
            }

            List<ComparisonRow> rows = new();

            foreach (Summary summary in summaries)
            {
                string summaryName = NormalizeSummaryName(summary.Title);

                foreach (BenchmarkReport report in summary.Reports)
                {
                    BenchmarkCase benchmarkCase = report.BenchmarkCase;
                    string methodName = NormalizeMethodName(
                        benchmarkCase.Descriptor.WorkloadMethodDisplayInfo
                    );
                    string parameterSignature = BuildParameterSignature(benchmarkCase);
                    BenchmarkKey key = new(summaryName, methodName, parameterSignature);

                    if (!baselineMetrics.TryGetValue(key, out BaselineMetrics baseline))
                    {
                        continue;
                    }

                    double novaMean = report.ResultStatistics?.Mean ?? double.NaN;
                    long? allocatedBytes = report.GcStats.GetBytesAllocatedPerOperation(
                        report.BenchmarkCase
                    );
                    double novaAllocated = allocatedBytes.HasValue
                        ? allocatedBytes.Value
                        : double.NaN;

                    rows.Add(
                        new ComparisonRow(
                            summaryName,
                            methodName,
                            BuildParameterDisplay(benchmarkCase),
                            novaMean,
                            baseline.MeanNanoseconds,
                            novaAllocated,
                            baseline.AllocatedBytes
                        )
                    );
                }
            }

            return rows.OrderBy(static r => r.SummaryName, StringComparer.Ordinal)
                .ThenBy(static r => r.Method, StringComparer.Ordinal)
                .ThenBy(static r => r.ParameterDisplay, StringComparer.Ordinal)
                .ToList();
        }

        private static string ReplaceSection(
            string documentContent,
            string osSectionName,
            string baselineBlock,
            string latestBlock
        )
        {
            string header = $"## {osSectionName}";
            string normalized = NormalizeLineEndings(documentContent);
            string newSectionContent = BuildSection(header, baselineBlock, latestBlock);

            List<DocumentSection> sections = ParseSections(normalized);
            bool replaced = false;

            for (int i = 0; i < sections.Count; i++)
            {
                if (string.Equals(sections[i].Header, header, StringComparison.Ordinal))
                {
                    if (!replaced)
                    {
                        sections[i] = new DocumentSection(header, newSectionContent);
                        replaced = true;
                    }
                    else
                    {
                        sections.RemoveAt(i);
                        i--;
                    }
                }
            }

            if (!replaced)
            {
                int insertIndex = sections.FindIndex(section =>
                    string.Equals(section.Header, "## Linux", StringComparison.Ordinal)
                    || string.Equals(section.Header, "## macOS", StringComparison.Ordinal)
                );

                DocumentSection newSection = new(header, newSectionContent);

                if (insertIndex >= 0)
                {
                    sections.Insert(insertIndex, newSection);
                }
                else
                {
                    sections.Add(newSection);
                }
            }

            string combined = ComposeDocument(sections);
            return NormalizeForWrite(combined);
        }

        private static List<DocumentSection> ParseSections(string normalizedContent)
        {
            List<DocumentSection> sections = new();
            StringBuilder builder = new();
            string? currentHeader = null;

            using StringReader reader = new(normalizedContent);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    if (builder.Length > 0)
                    {
                        sections.Add(
                            new DocumentSection(currentHeader, builder.ToString().TrimEnd('\n'))
                        );
                        builder.Clear();
                    }

                    currentHeader = line.Trim();
                }

                builder.AppendLine(line);
            }

            if (builder.Length > 0)
            {
                sections.Add(new DocumentSection(currentHeader, builder.ToString().TrimEnd('\n')));
            }

            return sections;
        }

        private static string ComposeDocument(IEnumerable<DocumentSection> sections)
        {
            StringBuilder builder = new();

            foreach (DocumentSection section in sections)
            {
                string content = section.Content.TrimEnd('\n');
                if (content.Length == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.Append(content);
            }

            return builder.ToString();
        }

        private static string BuildSection(string header, string baselineBlock, string latestBlock)
        {
            StringBuilder sectionBuilder = new();
            sectionBuilder.AppendLine(header);
            sectionBuilder.AppendLine();

            if (string.IsNullOrWhiteSpace(latestBlock))
            {
                sectionBuilder.AppendLine("_No NovaSharp benchmarks recorded yet._");
            }
            else
            {
                sectionBuilder.AppendLine(latestBlock.Trim());
            }

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
            sectionBuilder.AppendLine("---");
            sectionBuilder.AppendLine();

            if (string.IsNullOrWhiteSpace(baselineBlock))
            {
                sectionBuilder.AppendLine("_No MoonSharp baseline recorded yet._");
            }
            else
            {
                sectionBuilder.AppendLine(baselineBlock.Trim());
            }

            return sectionBuilder.ToString().TrimEnd();
        }

        private static string NormalizeLineEndings(string value) =>
            value.Replace("\r\n", "\n", StringComparison.Ordinal);

        private static string NormalizeForWrite(string value)
        {
            string normalized = NormalizeLineEndings(value).TrimEnd('\n');
            return (
                normalized.Replace("\n", Environment.NewLine, StringComparison.Ordinal)
                + Environment.NewLine
            );
        }

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

        private static Dictionary<BenchmarkKey, BaselineMetrics> ParseBaselineMetrics(
            string baselineBlock
        )
        {
            Dictionary<BenchmarkKey, BaselineMetrics> metrics = new();

            if (string.IsNullOrWhiteSpace(baselineBlock))
            {
                return metrics;
            }

            string normalized = NormalizeLineEndings(baselineBlock);
            using StringReader reader = new(normalized);

            string? line;
            string currentSummary = string.Empty;
            string[]? header = null;

            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("### ", StringComparison.Ordinal))
                {
                    currentSummary = NormalizeSummaryName(trimmed[4..]);
                    header = null;
                    continue;
                }

                if (!trimmed.StartsWith('|'))
                {
                    continue;
                }

                if (trimmed.StartsWith("|---", StringComparison.Ordinal))
                {
                    continue;
                }

                List<string> cells = ParseTableCells(trimmed);
                if (header == null)
                {
                    header = cells.ToArray();
                    continue;
                }

                if (header.Length == 0 || cells.Count != header.Length)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(currentSummary))
                {
                    continue;
                }

                int methodIndex = Array.IndexOf(header, "Method");
                int meanIndex = Array.IndexOf(header, "Mean");
                int allocIndex = Array.IndexOf(header, "Allocated");

                if (methodIndex < 0 || meanIndex < 0 || allocIndex < 0)
                {
                    continue;
                }

                string method = NormalizeMethodName(cells[methodIndex]);
                string parameterSignature = BuildParameterSignature(
                    header,
                    cells,
                    methodIndex,
                    meanIndex
                );

                double mean = ParseDurationToNanoseconds(cells[meanIndex]);
                double allocated = ParseBytesToDouble(cells[allocIndex]);

                if (double.IsNaN(mean) || double.IsNaN(allocated))
                {
                    continue;
                }

                BenchmarkKey key = new(currentSummary, method, parameterSignature);
                if (!metrics.ContainsKey(key))
                {
                    metrics.Add(key, new BaselineMetrics(mean, allocated));
                }
            }

            return metrics;
        }

        private static List<string> ParseTableCells(string row)
        {
            return row.Trim('|').Split('|').Select(static c => c.Trim()).ToList();
        }

        private static string NormalizeSummaryName(string value)
        {
            string normalized = NormalizeCell(value);
            normalized = Regex.Replace(normalized, "-\\d{8}-\\d{6}$", string.Empty);

            if (normalized.StartsWith("MoonSharp", StringComparison.Ordinal))
            {
                normalized = "NovaSharp" + normalized["MoonSharp".Length..];
            }

            return normalized;
        }

        private static string NormalizeMethodName(string value)
        {
            string normalized = NormalizeCell(value);

            if (normalized.StartsWith("MoonSharp", StringComparison.Ordinal))
            {
                normalized = "NovaSharp" + normalized["MoonSharp".Length..];
            }

            return normalized;
        }

        private static string NormalizeCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string decoded = WebUtility
                .HtmlDecode(value)
                .Replace("**", string.Empty, StringComparison.Ordinal)
                .Trim();

            decoded = decoded.Trim('\'', '"', '`');
            decoded = Regex.Replace(decoded, "\\s+", " ");

            return decoded;
        }

        private static string BuildParameterSignature(
            string[] header,
            List<string> cells,
            int methodIndex,
            int meanIndex
        )
        {
            if (methodIndex < 0 || meanIndex <= methodIndex + 1)
            {
                return string.Empty;
            }

            List<string> parts = new();

            for (int i = methodIndex + 1; i < meanIndex; i++)
            {
                string name = NormalizeCell(header[i]);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string value = NormalizeCell(cells[i]);
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                parts.Add($"{name}:{value}");
            }

            return string.Join("|", parts);
        }

        private static string BuildParameterSignature(BenchmarkCase benchmarkCase)
        {
            if (benchmarkCase.Parameters == null || benchmarkCase.Parameters.Items.Count == 0)
            {
                return string.Empty;
            }

            IEnumerable<string> parts = benchmarkCase
                .Parameters.Items.OrderBy(static p => p.Definition.Name, StringComparer.Ordinal)
                .Select(static p => $"{p.Definition.Name}:{NormalizeParameterValue(p.Value)}");

            return string.Join("|", parts);
        }

        private static string BuildParameterDisplay(BenchmarkCase benchmarkCase)
        {
            if (benchmarkCase.Parameters == null || benchmarkCase.Parameters.Items.Count == 0)
            {
                return "—";
            }

            IEnumerable<string> parts = benchmarkCase
                .Parameters.Items.OrderBy(static p => p.Definition.Name, StringComparer.Ordinal)
                .Select(static p => $"{p.Definition.Name}={NormalizeParameterValue(p.Value)}");

            return string.Join(", ", parts);
        }

        private static string NormalizeParameterValue(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            if (value is IFormattable formattable)
            {
                return NormalizeCell(
                    formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty
                );
            }

            return NormalizeCell(value.ToString() ?? string.Empty);
        }

        private static double ParseDurationToNanoseconds(string value)
        {
            string normalized = NormalizeCell(value);
            if (string.IsNullOrWhiteSpace(normalized) || normalized == "-")
            {
                return double.NaN;
            }

            Match match = Regex.Match(
                normalized,
                @"^([-+]?[0-9,]*\.?[0-9]*)\s*([a-zA-Zµμ]+)$",
                RegexOptions.CultureInvariant
            );

            if (!match.Success)
            {
                return double.NaN;
            }

            if (
                !double.TryParse(
                    match.Groups[1].Value.Replace(",", string.Empty, StringComparison.Ordinal),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double valuePart
                )
            )
            {
                return double.NaN;
            }

            string unit = match.Groups[2].Value;
            return unit switch
            {
                "ns" => valuePart,
                "μs" or "µs" or "us" => valuePart * 1_000d,
                "ms" => valuePart * 1_000_000d,
                "s" => valuePart * 1_000_000_000d,
                _ => double.NaN,
            };
        }

        private static double ParseBytesToDouble(string value)
        {
            string normalized = NormalizeCell(value);
            if (string.IsNullOrWhiteSpace(normalized) || normalized == "-")
            {
                return 0d;
            }

            Match match = Regex.Match(
                normalized,
                @"^([-+]?[0-9,]*\.?[0-9]*)\s*([A-Za-z]+)?$",
                RegexOptions.CultureInvariant
            );

            if (!match.Success)
            {
                return double.NaN;
            }

            if (
                !double.TryParse(
                    match.Groups[1].Value.Replace(",", string.Empty, StringComparison.Ordinal),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double valuePart
                )
            )
            {
                return double.NaN;
            }

            string unit = match.Groups[2].Success ? match.Groups[2].Value : "B";
            return unit switch
            {
                "B" => valuePart,
                "KB" => valuePart * 1024d,
                "MB" => valuePart * 1024d * 1024d,
                "GB" => valuePart * 1024d * 1024d * 1024d,
                _ => valuePart,
            };
        }

        private static string FormatTime(double nanoseconds)
        {
            if (double.IsNaN(nanoseconds))
            {
                return "—";
            }

            return FormatTimeMagnitude(Math.Abs(nanoseconds));
        }

        private static string FormatTimeDifference(double nanoseconds)
        {
            if (double.IsNaN(nanoseconds))
            {
                return "—";
            }

            double abs = Math.Abs(nanoseconds);
            if (abs < double.Epsilon)
            {
                return "0 ns";
            }

            string formatted = FormatTimeMagnitude(abs);
            return nanoseconds >= 0 ? $"+{formatted}" : $"-{formatted}";
        }

        private static string FormatTimeMagnitude(double nanoseconds)
        {
            double value = nanoseconds;
            string unit = "ns";

            if (value >= 1_000_000_000d)
            {
                value /= 1_000_000_000d;
                unit = "s";
            }
            else if (value >= 1_000_000d)
            {
                value /= 1_000_000d;
                unit = "ms";
            }
            else if (value >= 1_000d)
            {
                value /= 1_000d;
                unit = "us";
            }

            string format =
                value >= 100d ? "N0"
                : value >= 10d ? "N1"
                : "N3";
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}",
                value.ToString(format, CultureInfo.InvariantCulture),
                unit
            );
        }

        private static string FormatBytes(double bytes)
        {
            if (double.IsNaN(bytes))
            {
                return "—";
            }

            return FormatBytesMagnitude(Math.Abs(bytes));
        }

        private static string FormatBytesDifference(double bytes)
        {
            if (double.IsNaN(bytes))
            {
                return "—";
            }

            double abs = Math.Abs(bytes);
            if (abs < double.Epsilon)
            {
                return "0 B";
            }

            string formatted = FormatBytesMagnitude(abs);
            return bytes >= 0 ? $"+{formatted}" : $"-{formatted}";
        }

        private static string FormatBytesMagnitude(double bytes)
        {
            double value = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unitIndex = 0;

            while (value >= 1024d && unitIndex < units.Length - 1)
            {
                value /= 1024d;
                unitIndex++;
            }

            string format =
                value >= 100d ? "N0"
                : value >= 10d ? "N1"
                : "N2";
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1}",
                value.ToString(format, CultureInfo.InvariantCulture),
                units[unitIndex]
            );
        }

        private static string FormatPercentage(double percentage)
        {
            if (double.IsNaN(percentage) || double.IsInfinity(percentage))
            {
                return "—";
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:+0.##;-0.##;0}%", percentage);
        }

        private static string DeriveEditionLabel(string? productName, string? editionId)
        {
            string? edition = null;

            if (!string.IsNullOrWhiteSpace(productName))
            {
                string trimmed = Regex.Replace(
                    productName,
                    @"^\s*Windows\s+\d+\s*",
                    string.Empty,
                    RegexOptions.IgnoreCase
                );
                trimmed = Regex.Replace(
                    trimmed,
                    @"^\s*Windows\s*",
                    string.Empty,
                    RegexOptions.IgnoreCase
                );

                edition = trimmed.Trim();
            }

            if (string.IsNullOrWhiteSpace(edition) && !string.IsNullOrWhiteSpace(editionId))
            {
                edition = editionId switch
                {
                    "Professional" => "Pro",
                    "ProfessionalEducation" => "Pro Education",
                    "ProfessionalSingleLanguage" => "Pro Single Language",
                    "ProfessionalCountrySpecific" => "Pro Country Specific",
                    "ProfessionalWorkstation" => "Pro for Workstations",
                    "Enterprise" => "Enterprise",
                    "EnterpriseN" => "Enterprise N",
                    "Education" => "Education",
                    "EducationN" => "Education N",
                    "Core" => "Home",
                    "CoreN" => "Home N",
                    "CoreCountrySpecific" => "Home Country Specific",
                    "CoreSingleLanguage" => "Home Single Language",
                    "ServerStandard" => "Server Standard",
                    "ServerDatacenter" => "Server Datacenter",
                    _ => Regex.Replace(
                        editionId,
                        "(?<=[A-Za-z])(?=[A-Z])",
                        " ",
                        RegexOptions.Compiled
                    ),
                };
            }

            return string.IsNullOrWhiteSpace(edition) ? string.Empty : edition;
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
            catch (InvalidOperationException)
            {
                // ignore – best-effort metric
            }
            catch (PlatformNotSupportedException)
            {
                // ignore – best-effort metric
            }

            return 0;
        }

        private readonly record struct BenchmarkKey(
            string SummaryName,
            string Method,
            string Parameters
        );

        private readonly record struct BaselineMetrics(
            double MeanNanoseconds,
            double AllocatedBytes
        );

        private sealed record ComparisonRow(
            string SummaryName,
            string Method,
            string ParameterDisplay,
            double NovaMeanNanoseconds,
            double BaselineMeanNanoseconds,
            double NovaAllocatedBytes,
            double BaselineAllocatedBytes
        )
        {
            public double MeanDeltaNanoseconds => NovaMeanNanoseconds - BaselineMeanNanoseconds;

            public double AllocatedDeltaBytes => NovaAllocatedBytes - BaselineAllocatedBytes;

            public double MeanDeltaPercent =>
                BaselineMeanNanoseconds > 0
                    ? (MeanDeltaNanoseconds / BaselineMeanNanoseconds) * 100d
                    : double.NaN;

            public double AllocatedDeltaPercent =>
                BaselineAllocatedBytes > 0
                    ? (AllocatedDeltaBytes / BaselineAllocatedBytes) * 100d
                    : double.NaN;
        }

        private sealed record DocumentSection(string? Header, string Content);
    }
}
