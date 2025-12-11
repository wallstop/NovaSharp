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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;

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

        public SafePlatformAccessor(IPlatformAccessor inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
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

        public int ExecuteCommand(string cmdline) => _inner.ExecuteCommand(cmdline);

        public void ExitFast(int exitCode)
        {
            // Throw instead of exiting, so the batch runner can continue
            throw new ScriptExitException(exitCode);
        }
    }

    internal static class Program
    {
        private const int ScriptTimeoutMs = 5000; // 5 second timeout per script

        internal static int Main(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                Console.Error.WriteLine("Usage: LuaBatchRunner <output-dir> <file.lua>...");
                Console.Error.WriteLine(
                    "   or: LuaBatchRunner <output-dir> --files-from <list.txt>"
                );
                return 1;
            }

            string outputDir = args[0];
            List<string> luaFiles = new List<string>();

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

            // Install safe platform that throws on os.exit() instead of terminating
            IPlatformAccessor originalPlatform = Script.GlobalOptions.Platform;
            Script.GlobalOptions.Platform = new SafePlatformAccessor(originalPlatform);

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

                                // Run script with timeout to handle infinite loops
                                Exception caughtException = null;
                                Task scriptTask = Task.Run(() =>
                                {
                                    try
                                    {
                                        Script script = new Script();
                                        script.DoFile(luaFile);
                                    }
#pragma warning disable CA1031 // Catch all exceptions from user scripts intentionally
                                    catch (Exception ex)
                                    {
                                        caughtException = ex;
                                    }
#pragma warning restore CA1031
                                });

                                bool completed = scriptTask.Wait(ScriptTimeoutMs);
                                if (!completed)
                                {
                                    stderrWriter.WriteLine(
                                        "Script execution timed out after 5 seconds"
                                    );
                                    returnCode = 4;
                                    timeoutCount++;
                                }
                                else if (caughtException != null)
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

                    File.WriteAllText(outFile, stdout);
                    File.WriteAllText(errFile, stderr);
                    File.WriteAllText(rcFile, returnCode.ToString(CultureInfo.InvariantCulture));

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
