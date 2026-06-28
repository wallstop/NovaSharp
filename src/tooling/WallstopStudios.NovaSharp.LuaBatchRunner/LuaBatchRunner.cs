// LuaBatchRunner.cs - Runs multiple Lua files in a single process for fast comparison testing
//
// Usage: dotnet run -- <output-dir> <file1.lua> <file2.lua> ...
// Or:    dotnet run -- <output-dir> --files-from <list.txt>
//
// Output: For each input file, creates:
//   - <output-dir>/<relative-path>.nova.out (stdout)
//   - <output-dir>/<relative-path>.nova.err (stderr)
//   - <output-dir>/<relative-path>.nova.rc (return code: 0=success, 1=runtime error, 2=parse error, 4=timeout)

namespace WallstopStudios.NovaSharp.LuaBatchRunner
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Exception thrown when a script calls os.exit() instead of terminating the process.
    /// </summary>
    internal sealed class ScriptExitException : Exception
    {
        public int ExitCode { get; }

        public ScriptExitException()
            : base("Script called os.exit()")
        {
            ExitCode = 0;
        }

        public ScriptExitException(string message)
            : base(message)
        {
            ExitCode = 0;
        }

        public ScriptExitException(string message, Exception innerException)
            : base(message, innerException)
        {
            ExitCode = 0;
        }

        public ScriptExitException(int exitCode)
            : base(
                string.Format(CultureInfo.InvariantCulture, "Script called os.exit({0})", exitCode)
            )
        {
            ExitCode = exitCode;
        }
    }

    /// <summary>
    /// Platform accessor wrapper that throws on os.exit() instead of terminating the process.
    /// </summary>
    internal sealed class SafePlatformAccessor : IPlatformAccessor
    {
        private readonly IPlatformAccessor _inner;
        private readonly TimeSpan _commandTimeout;
        private readonly Func<TimeSpan> _getRemainingFixtureTime;
        private readonly Func<string> _formatFixtureTimeoutMessage;

        public SafePlatformAccessor(
            IPlatformAccessor inner,
            TimeSpan commandTimeout,
            Func<TimeSpan> getRemainingFixtureTime,
            Func<string> formatFixtureTimeoutMessage
        )
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _commandTimeout = commandTimeout;
            _getRemainingFixtureTime = getRemainingFixtureTime;
            _formatFixtureTimeoutMessage = formatFixtureTimeoutMessage;
        }

        public CoreModules FilterSupportedCoreModules(CoreModules coreModules) =>
            _inner.FilterSupportedCoreModules(coreModules);

        public string GetEnvironmentVariable(string envvarname) =>
            _inner.GetEnvironmentVariable(envvarname);

        public bool IsRunningOnAOT() => _inner.IsRunningOnAOT();

        public string GetPlatformName() => _inner.GetPlatformName();

        public void DefaultPrint(string content) => _inner.DefaultPrint(content);

        public string DefaultInput(string prompt) => _inner.DefaultInput(prompt);

        public Stream OpenFile(Script script, string filename, Encoding encoding, string mode) =>
            _inner.OpenFile(script, filename, encoding, mode);

        public Stream GetStandardStream(StandardFileType type) => _inner.GetStandardStream(type);

        public string GetTempFileName() => _inner.GetTempFileName();

        public bool FileExists(string file) => _inner.FileExists(file);

        public void DeleteFile(string file) => _inner.DeleteFile(file);

        public void MoveFile(string src, string dst) => _inner.MoveFile(src, dst);

        public int ExecuteCommand(string cmdline)
        {
            bool hasFixtureTimeout = _getRemainingFixtureTime != null;
            if (_commandTimeout <= TimeSpan.Zero && !hasFixtureTimeout)
            {
                return _inner.ExecuteCommand(cmdline);
            }

            if (string.IsNullOrWhiteSpace(cmdline))
            {
                return 0;
            }

            ProcessStartInfo startInfo = BuildShellProcessStartInfo(cmdline);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            try
            {
                TimeSpan effectiveTimeout = GetEffectiveCommandTimeout(
                    out bool fixtureTimeoutBound
                );
                using Process process =
                    Process.Start(startInfo)
                    ?? throw new InvalidOperationException("Failed to start command process.");
                if (process.WaitForExit(effectiveTimeout))
                {
                    ThrowIfFixtureTimedOut();
                    return process.ExitCode;
                }

                KillProcessTree(process);
                if (fixtureTimeoutBound)
                {
                    ThrowIfFixtureTimedOut(force: true);
                }

                throw new TimeoutException(FormatCommandTimeoutMessage(cmdline));
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
        }

        private TimeSpan GetEffectiveCommandTimeout(out bool fixtureTimeoutBound)
        {
            fixtureTimeoutBound = false;
            TimeSpan timeout =
                _commandTimeout > TimeSpan.Zero ? _commandTimeout : TimeSpan.MaxValue;

            if (_getRemainingFixtureTime == null)
            {
                return timeout;
            }

            TimeSpan remaining = _getRemainingFixtureTime();
            if (remaining <= TimeSpan.Zero)
            {
                ThrowIfFixtureTimedOut(force: true);
            }

            if (remaining < timeout)
            {
                fixtureTimeoutBound = true;
                return remaining;
            }

            return timeout;
        }

        private void ThrowIfFixtureTimedOut(bool force = false)
        {
            if (_getRemainingFixtureTime == null)
            {
                return;
            }

            if (force || _getRemainingFixtureTime() <= TimeSpan.Zero)
            {
                string message =
                    _formatFixtureTimeoutMessage != null
                        ? _formatFixtureTimeoutMessage()
                        : "Script execution timed out";
                throw new TimeoutException(message);
            }
        }

        private string FormatCommandTimeoutMessage(string cmdline)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Command execution timed out after {0:g} seconds: {1}",
                _commandTimeout.TotalSeconds,
                cmdline
            );
        }

        public void ExitFast(int exitCode)
        {
            // Throw instead of exiting, so the batch runner can continue
            throw new ScriptExitException(exitCode);
        }

        private static ProcessStartInfo BuildShellProcessStartInfo(string cmdline)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.FileName = "cmd.exe";
                startInfo.ArgumentList.Add("/C");
                startInfo.ArgumentList.Add(cmdline);
            }
            else
            {
                startInfo.FileName = "/bin/sh";
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add(cmdline);
            }

            return startInfo;
        }

        private static void KillProcessTree(Process process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
            catch (InvalidOperationException)
            {
                // The process exited between the timeout check and kill attempt.
            }
        }
    }

    internal static class Program
    {
        private const long DefaultMaxInstructions = 50_000_000;
        private const int DefaultMaxWallClockSeconds = 5;
        private const int DefaultMaxCommandSeconds = 5;
        private const long WallClockInstructionCheckInterval = 50_000;
        private static readonly char[] VersionSplitChars = new char[] { ',', ' ' };

        /// <summary>
        /// Tries to parse a Lua version string into a <see cref="LuaCompatibilityVersion"/>.
        /// </summary>
        /// <param name="versionString">The version string to parse (e.g., "5.4", "54", "Lua54").</param>
        /// <param name="version">The parsed version.</param>
        /// <returns>True if parsing succeeded.</returns>
        private static bool TryParseLuaVersion(
            string versionString,
            out LuaCompatibilityVersion version
        )
        {
            version = LuaCompatibilityVersion.Latest;

            if (string.IsNullOrWhiteSpace(versionString))
            {
                return false;
            }

            // Normalize: remove "lua" prefix if present, trim whitespace
            string normalized = versionString.Trim();
            if (
                normalized.StartsWith("lua", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("Lua", StringComparison.Ordinal)
            )
            {
                normalized = normalized.Substring(3);
            }

            // Remove dots: "5.4" -> "54"
            normalized = normalized.Replace(".", string.Empty, StringComparison.Ordinal);

            // Try parse as integer
            if (
                int.TryParse(
                    normalized,
                    System.Globalization.NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int numericVersion
                )
            )
            {
                switch (numericVersion)
                {
                    case 51:
                        version = LuaCompatibilityVersion.Lua51;
                        return true;
                    case 52:
                        version = LuaCompatibilityVersion.Lua52;
                        return true;
                    case 53:
                        version = LuaCompatibilityVersion.Lua53;
                        return true;
                    case 54:
                        version = LuaCompatibilityVersion.Lua54;
                        return true;
                    case 55:
                        version = LuaCompatibilityVersion.Lua55;
                        return true;
                }
            }

            // Try parse "latest"
            if (string.Equals(normalized, "latest", StringComparison.OrdinalIgnoreCase))
            {
                version = LuaCompatibilityVersion.Latest;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parses the @lua-versions metadata from the first few lines of a Lua fixture file.
        /// </summary>
        /// <param name="luaFilePath">Path to the Lua file.</param>
        /// <returns>The first valid version found, or null if no version metadata exists.</returns>
        private static LuaCompatibilityVersion? ParseLuaVersionFromFile(string luaFilePath)
        {
            const int MaxLinesToCheck = 10;

            try
            {
                using (StreamReader reader = new StreamReader(luaFilePath))
                {
                    for (int lineNum = 0; lineNum < MaxLinesToCheck; lineNum++)
                    {
                        string line = reader.ReadLine();
                        if (line == null)
                        {
                            break;
                        }

                        // Look for @lua-versions: in comments
                        // Format: -- @lua-versions: 5.1, 5.2, 5.3
                        int idx = line.IndexOf(
                            "@lua-versions:",
                            StringComparison.OrdinalIgnoreCase
                        );
                        if (idx >= 0)
                        {
                            string versionsPart = line.Substring(idx + "@lua-versions:".Length)
                                .Trim();
                            // Take the first version listed
                            string[] versions = versionsPart.Split(
                                VersionSplitChars,
                                StringSplitOptions.RemoveEmptyEntries
                            );
                            if (versions.Length > 0)
                            {
                                if (
                                    TryParseLuaVersion(
                                        versions[0],
                                        out LuaCompatibilityVersion parsedVersion
                                    )
                                )
                                {
                                    return parsedVersion;
                                }
                            }
                        }
                    }
                }
            }
#pragma warning disable CA1031 // Catch all exceptions when reading file metadata
            catch (Exception)
            {
                // If we can't read the file, we'll let the main execution handle the error
            }
#pragma warning restore CA1031

            return null;
        }

        internal static int Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Console.Error.WriteLine("Usage: LuaBatchRunner <output-dir> <file.lua>...");
                Console.Error.WriteLine(
                    "   or: LuaBatchRunner <output-dir> --files-from <list.txt>"
                );
                Console.Error.WriteLine(
                    "       --lua-version <5.1|5.2|5.3|5.4|5.5>  Set Lua compatibility version"
                );
                Console.Error.WriteLine(
                    "       --max-instructions <n>                Per-fixture VM instruction limit; 0 disables it"
                );
                Console.Error.WriteLine(
                    "       --max-wall-seconds <n>                Per-fixture wall-clock timeout; 0 disables it"
                );
                Console.Error.WriteLine(
                    "       --max-command-seconds <n>             Per-fixture os.execute timeout; 0 disables it"
                );
                return 1;
            }

            string outputDir = args[0];
            List<string> luaFiles = new List<string>();
            LuaCompatibilityVersion? cliLuaVersion = null;
            long maxInstructions = DefaultMaxInstructions;
            int maxWallClockSeconds = DefaultMaxWallClockSeconds;
            int maxCommandSeconds = DefaultMaxCommandSeconds;

            // Parse arguments
            int i = 1;
            while (i < args.Length)
            {
                if (args[i] == "--files-from" && i + 1 < args.Length)
                {
                    string listFile = args[i + 1];
                    if (File.Exists(listFile))
                    {
                        luaFiles.AddRange(
                            File.ReadAllLines(listFile).Where(l => !string.IsNullOrWhiteSpace(l))
                        );
                    }
                    i += 2;
                }
                else if (args[i] == "--base-dir" && i + 1 < args.Length)
                {
                    // Base directory for relative path calculation (ignored, we use full paths)
                    i += 2;
                }
                else if (args[i] == "--lua-version" && i + 1 < args.Length)
                {
                    if (TryParseLuaVersion(args[i + 1], out LuaCompatibilityVersion parsedVersion))
                    {
                        cliLuaVersion = parsedVersion;
                    }
                    else
                    {
                        Console.Error.WriteLine(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Warning: Invalid Lua version '{0}', using default",
                                args[i + 1]
                            )
                        );
                    }
                    i += 2;
                }
                else if (args[i] == "--max-instructions" && i + 1 < args.Length)
                {
                    if (
                        !long.TryParse(
                            args[i + 1],
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out long parsedMaxInstructions
                        )
                        || parsedMaxInstructions < 0
                    )
                    {
                        Console.Error.WriteLine(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Invalid --max-instructions value '{0}'",
                                args[i + 1]
                            )
                        );
                        return 1;
                    }

                    maxInstructions = parsedMaxInstructions;
                    i += 2;
                }
                else if (args[i] == "--max-wall-seconds" && i + 1 < args.Length)
                {
                    if (
                        !int.TryParse(
                            args[i + 1],
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out int parsedMaxWallClockSeconds
                        )
                        || parsedMaxWallClockSeconds < 0
                    )
                    {
                        Console.Error.WriteLine(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Invalid --max-wall-seconds value '{0}'",
                                args[i + 1]
                            )
                        );
                        return 1;
                    }

                    maxWallClockSeconds = parsedMaxWallClockSeconds;
                    i += 2;
                }
                else if (args[i] == "--max-command-seconds" && i + 1 < args.Length)
                {
                    if (
                        !int.TryParse(
                            args[i + 1],
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out int parsedMaxCommandSeconds
                        )
                        || parsedMaxCommandSeconds < 0
                    )
                    {
                        Console.Error.WriteLine(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Invalid --max-command-seconds value '{0}'",
                                args[i + 1]
                            )
                        );
                        return 1;
                    }

                    maxCommandSeconds = parsedMaxCommandSeconds;
                    i += 2;
                }
                else
                {
                    luaFiles.Add(args[i]);
                    i++;
                }
            }

            if (luaFiles.Count == 0)
            {
                Console.Error.WriteLine("No Lua files specified.");
                return 1;
            }

            Directory.CreateDirectory(outputDir);

            TimeSpan maxWallClock =
                maxWallClockSeconds > 0 ? TimeSpan.FromSeconds(maxWallClockSeconds) : TimeSpan.Zero;
            Stopwatch currentFixtureStopwatch = null;

            // Install safe platform that throws on os.exit() instead of terminating
            IPlatformAccessor originalPlatform = Script.GlobalOptions.Platform;
            Script.GlobalOptions.Platform = new SafePlatformAccessor(
                originalPlatform,
                TimeSpan.FromSeconds(maxCommandSeconds),
                maxWallClock > TimeSpan.Zero
                    ? () =>
                    {
                        if (currentFixtureStopwatch == null)
                        {
                            return TimeSpan.MaxValue;
                        }

                        return maxWallClock - currentFixtureStopwatch.Elapsed;
                    }
                    : null,
                maxWallClock > TimeSpan.Zero
                    ? () => FormatFixtureTimeoutMessage(maxWallClock)
                    : null
            );

            int passCount = 0;
            int failCount = 0;
            int errorCount = 0;
            int timeoutCount = 0;
            int processed = 0;

            DateTime startTime = DateTime.UtcNow;

            try
            {
                foreach (string luaFile in luaFiles)
                {
                    processed++;

                    // Compute output paths
                    string outBase;
                    int fixturesIdx = luaFile.IndexOf(
                        "LuaFixtures",
                        StringComparison.OrdinalIgnoreCase
                    );
                    if (fixturesIdx >= 0)
                    {
                        string relativePart = luaFile
                            .Substring(fixturesIdx + "LuaFixtures".Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        relativePart = Path.ChangeExtension(relativePart, null);
                        outBase = Path.Combine(outputDir, relativePart);
                    }
                    else
                    {
                        outBase = Path.Combine(
                            outputDir,
                            Path.GetFileNameWithoutExtension(luaFile)
                        );
                    }

                    string outDir = Path.GetDirectoryName(outBase);
                    if (!string.IsNullOrEmpty(outDir))
                    {
                        Directory.CreateDirectory(outDir);
                    }

                    string outFile = outBase + ".nova.out";
                    string errFile = outBase + ".nova.err";
                    string rcFile = outBase + ".nova.rc";

                    string stdout = "";
                    string stderr = "";
                    int returnCode = 0;

                    // Determine Lua version: CLI arg takes precedence, then file metadata, then default
                    LuaCompatibilityVersion effectiveVersion;
                    if (cliLuaVersion.HasValue)
                    {
                        effectiveVersion = cliLuaVersion.Value;
                    }
                    else
                    {
                        LuaCompatibilityVersion? fileVersion = ParseLuaVersionFromFile(luaFile);
                        effectiveVersion = fileVersion ?? LuaCompatibilityVersion.Lua51;
                    }

                    Stopwatch fixtureStopwatch = Stopwatch.StartNew();
                    currentFixtureStopwatch = fixtureStopwatch;

                    try
                    {
                        // Capture console output
                        using (
                            StringWriter stdoutWriter = new StringWriter(
                                CultureInfo.InvariantCulture
                            )
                        )
                        using (
                            StringWriter stderrWriter = new StringWriter(
                                CultureInfo.InvariantCulture
                            )
                        )
                        {
                            TextWriter originalOut = Console.Out;
                            TextWriter originalErr = Console.Error;

                            try
                            {
                                Console.SetOut(stdoutWriter);
                                Console.SetError(stderrWriter);

                                Exception caughtException = null;
                                try
                                {
                                    ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
                                    {
                                        CompatibilityVersion = effectiveVersion,
                                    };
                                    SandboxOptions sandbox = CreateSandboxOptions(
                                        maxInstructions,
                                        maxWallClock,
                                        fixtureStopwatch
                                    );
                                    options.Sandbox = sandbox;

                                    Script script = new Script(CoreModulePresets.Complete, options);
                                    script.DoFile(luaFile);
                                    ThrowIfFixtureWallClockExceeded(maxWallClock, fixtureStopwatch);
                                }
#pragma warning disable CA1031 // Catch all exceptions from user scripts intentionally
                                catch (Exception ex)
                                {
                                    caughtException = ex;
                                }
#pragma warning restore CA1031

                                if (caughtException != null)
                                {
                                    // Handle the caught exception
                                    if (caughtException is ScriptExitException exitEx)
                                    {
                                        // os.exit() is considered success with the exit code
                                        returnCode = exitEx.ExitCode;
                                        passCount++;
                                    }
                                    else if (caughtException is SyntaxErrorException sex)
                                    {
                                        stderrWriter.WriteLine(sex.Message);
                                        returnCode = 2;
                                        failCount++;
                                    }
                                    else if (
                                        caughtException is SandboxViolationException sandboxEx
                                        && sandboxEx.ViolationType
                                            == SandboxViolationType.InstructionLimitExceeded
                                    )
                                    {
                                        stderrWriter.WriteLine(
                                            string.Format(
                                                CultureInfo.InvariantCulture,
                                                "Script execution exceeded {0} instructions",
                                                sandboxEx.ConfiguredLimit
                                            )
                                        );
                                        returnCode = 4;
                                        timeoutCount++;
                                    }
                                    else if (
                                        caughtException
                                        is SandboxViolationException sandboxViolation
                                    )
                                    {
                                        stderrWriter.WriteLine(sandboxViolation.Message);
                                        returnCode = 3;
                                        errorCount++;
                                    }
                                    else if (
                                        TryGetTimeoutException(
                                            caughtException,
                                            out TimeoutException timeoutException
                                        )
                                    )
                                    {
                                        stderrWriter.WriteLine(timeoutException.Message);
                                        returnCode = 4;
                                        timeoutCount++;
                                    }
                                    else if (caughtException is ScriptRuntimeException rex)
                                    {
                                        stderrWriter.WriteLine(rex.DecoratedMessage ?? rex.Message);
                                        returnCode = 1;
                                        failCount++;
                                    }
                                    else
                                    {
                                        stderrWriter.WriteLine(
                                            string.Format(
                                                CultureInfo.InvariantCulture,
                                                "Unexpected error: {0}: {1}",
                                                caughtException.GetType().Name,
                                                caughtException.Message
                                            )
                                        );
                                        returnCode = 3;
                                        errorCount++;
                                    }
                                }
                                else
                                {
                                    returnCode = 0;
                                    passCount++;
                                }
                            }
                            catch (SyntaxErrorException sex)
                            {
                                stderrWriter.WriteLine(sex.Message);
                                returnCode = 2;
                                failCount++;
                            }
                            catch (ScriptRuntimeException rex)
                            {
                                stderrWriter.WriteLine(rex.DecoratedMessage ?? rex.Message);
                                returnCode = 1;
                                failCount++;
                            }
                            catch (IOException ioex)
                            {
                                stderrWriter.WriteLine(
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        "IO error: {0}: {1}",
                                        ioex.GetType().Name,
                                        ioex.Message
                                    )
                                );
                                returnCode = 3;
                                errorCount++;
                            }
                            catch (UnauthorizedAccessException uaex)
                            {
                                stderrWriter.WriteLine(
                                    string.Format(
                                        CultureInfo.InvariantCulture,
                                        "Access error: {0}",
                                        uaex.Message
                                    )
                                );
                                returnCode = 3;
                                errorCount++;
                            }
                            finally
                            {
                                Console.SetOut(originalOut);
                                Console.SetError(originalErr);
                                stdout = stdoutWriter.ToString();
                                stderr = stderrWriter.ToString();
                            }
                        }
                    }
                    catch (IOException ioex)
                    {
                        stderr = string.Format(
                            CultureInfo.InvariantCulture,
                            "Failed to process file: {0}",
                            ioex.Message
                        );
                        returnCode = 3;
                        errorCount++;
                    }
                    catch (OperationCanceledException)
                    {
                        stderr = "Script execution was cancelled";
                        returnCode = 4;
                        timeoutCount++;
                    }
                    catch (TimeoutException timeoutException)
                    {
                        stderr = timeoutException.Message;
                        returnCode = 4;
                        timeoutCount++;
                    }

                    File.WriteAllText(outFile, stdout);
                    File.WriteAllText(errFile, stderr);
                    File.WriteAllText(rcFile, returnCode.ToString(CultureInfo.InvariantCulture));
                    currentFixtureStopwatch = null;

                    if (processed % 100 == 0)
                    {
                        double elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
                        double rate = processed / elapsed;
                        double remaining = (luaFiles.Count - processed) / rate;
                        Console.Error.WriteLine(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "  Progress: {0}/{1} ({2:F1}/s, ~{3:F0}s remaining)",
                                processed,
                                luaFiles.Count,
                                rate,
                                remaining
                            )
                        );
                    }
                }
            }
#pragma warning disable CA1031 // Catch all exceptions to ensure summary is written
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Fatal error after processing {0} files: {1}",
                        processed,
                        ex.Message
                    )
                );
            }
#pragma warning restore CA1031
            finally
            {
                WriteSummary(
                    outputDir,
                    processed,
                    passCount,
                    failCount,
                    errorCount,
                    timeoutCount,
                    startTime
                );
            }

            return 0;
        }

        private static bool TryGetTimeoutException(
            Exception exception,
            out TimeoutException timeoutException
        )
        {
            while (exception != null)
            {
                if (exception is TimeoutException currentTimeoutException)
                {
                    timeoutException = currentTimeoutException;
                    return true;
                }

                exception = exception.InnerException;
            }

            timeoutException = null;
            return false;
        }

        private static SandboxOptions CreateSandboxOptions(
            long maxInstructions,
            TimeSpan maxWallClock,
            Stopwatch fixtureStopwatch
        )
        {
            if (maxWallClock <= TimeSpan.Zero)
            {
                return maxInstructions > 0
                    ? new SandboxOptions { MaxInstructions = maxInstructions }
                    : SandboxOptions.Unrestricted;
            }

            long instructionCheckInterval =
                maxInstructions > 0
                    ? Math.Min(maxInstructions, WallClockInstructionCheckInterval)
                    : WallClockInstructionCheckInterval;
            long totalInstructions = 0;

            return new SandboxOptions
            {
                MaxInstructions = instructionCheckInterval,
                OnInstructionLimitExceeded = (_, executedInstructions) =>
                {
                    totalInstructions += executedInstructions;
                    ThrowIfFixtureWallClockExceeded(maxWallClock, fixtureStopwatch);

                    if (maxInstructions > 0 && totalInstructions > maxInstructions)
                    {
                        throw new SandboxViolationException(
                            SandboxViolationType.InstructionLimitExceeded,
                            maxInstructions,
                            totalInstructions
                        );
                    }

                    return true;
                },
            };
        }

        private static void ThrowIfFixtureWallClockExceeded(
            TimeSpan maxWallClock,
            Stopwatch fixtureStopwatch
        )
        {
            if (
                maxWallClock <= TimeSpan.Zero
                || fixtureStopwatch == null
                || fixtureStopwatch.Elapsed <= maxWallClock
            )
            {
                return;
            }

            throw new TimeoutException(FormatFixtureTimeoutMessage(maxWallClock));
        }

        private static string FormatFixtureTimeoutMessage(TimeSpan maxWallClock)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Script execution timed out after {0:g} seconds",
                maxWallClock.TotalSeconds
            );
        }

        private static void WriteSummary(
            string outputDir,
            int processed,
            int passCount,
            int failCount,
            int errorCount,
            int timeoutCount,
            DateTime startTime
        )
        {
            double totalElapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            Console.Error.WriteLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Completed {0} files in {1:F1}s: {2} pass, {3} fail, {4} error, {5} timeout",
                    processed,
                    totalElapsed,
                    passCount,
                    failCount,
                    errorCount,
                    timeoutCount
                )
            );

            // Write summary
            string summaryFile = Path.Combine(outputDir, "novasharp_summary.json");
            File.WriteAllText(
                summaryFile,
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"{{
  ""total"": {0},
  ""pass"": {1},
  ""fail"": {2},
  ""error"": {3},
  ""timeout"": {4},
  ""elapsed_seconds"": {5:F2}
}}",
                    processed,
                    passCount,
                    failCount,
                    errorCount,
                    timeoutCount,
                    totalElapsed
                )
            );
        }
    }
}
