namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    internal static class TapStdinHelper
    {
        private const string HelperName = "platform.stdin_helper.run";

        internal static string ResolveInputPathForTests(
            string relativePath,
            string workingDirectory,
            string testDirectory,
            string baseDirectory,
            string runtimeDirectory
        )
        {
            return ResolveInputPathCore(
                relativePath,
                workingDirectory,
                testDirectory,
                baseDirectory,
                runtimeDirectory
            );
        }

        public static void Register(
            Script script,
            Table platform,
            LuaCompatibilityVersion compatibilityVersion,
            string testDirectory
        )
        {
            if (script == null || platform == null)
            {
                return;
            }

            TapStdinHelperContext context = new(compatibilityVersion, testDirectory);
            Table helperTable = new(script);
            CallbackFunction runFunction = new(Run, HelperName) { AdditionalData = context };
            helperTable.Set("run", DynValue.NewCallback(runFunction));
            platform.Set("stdin_helper", DynValue.NewTable(helperTable));
        }

        private static DynValue Run(ScriptExecutionContext executionContext, CallbackArguments args)
        {
            ArgumentNullException.ThrowIfNull(executionContext);
            TapStdinHelperContext context =
                executionContext.AdditionalData as TapStdinHelperContext;

            if (context == null)
            {
                throw new InvalidOperationException("stdin helper context is unavailable.");
            }

            DynValue chunkValue = args.AsType(0, HelperName, DataType.String, false);
            DynValue inputValue = args.AsType(1, HelperName, DataType.String, false);
            string absoluteInputPath = context.ResolveInputPath(inputValue.String);

            List<string> outputLines = ExecuteChunk(
                chunkValue.String,
                absoluteInputPath,
                context.CompatibilityVersion
            );

            Table result = new(executionContext.Script);

            for (int index = 0; index < outputLines.Count; index++)
            {
                result.Set(index + 1, DynValue.NewString(outputLines[index]));
            }

            return DynValue.NewTable(result);
        }

        private static List<string> ExecuteChunk(
            string chunk,
            string inputPath,
            LuaCompatibilityVersion compatibilityVersion
        )
        {
            if (!File.Exists(inputPath))
            {
                throw new ScriptRuntimeException($"stdin helper could not locate '{inputPath}'.");
            }

            string stdinText = File.ReadAllText(inputPath, Encoding.UTF8);
            byte[] stdinBytes = Encoding.UTF8.GetBytes(stdinText);

            using MemoryStream stdinStream = new(stdinBytes, writable: false);
            Queue<string> debugCommands = BuildDebugInputQueue(stdinText);
            List<string> outputLines = new();

            ScriptOptions options = new(Script.DefaultOptions)
            {
                CompatibilityVersion = compatibilityVersion,
                Stdin = stdinStream,
                DebugPrint = message =>
                {
                    // Treat messages routed through DebugPrint as stderr-equivalents so they do not
                    // appear in the captured stdout output.
                },
                DebugInput = _ =>
                {
                    if (debugCommands.Count == 0)
                    {
                        return null;
                    }

                    return debugCommands.Dequeue();
                },
            };

            Script script = new(CoreModulePresets.Complete, options);
            script.Globals.Set(
                "print",
                DynValue.NewCallback(
                    (context, arguments) =>
                    {
                        StringBuilder builder = new StringBuilder();

                        for (int i = 0; i < arguments.Count; i++)
                        {
                            if (arguments[i].IsVoid())
                            {
                                break;
                            }

                            if (i != 0)
                            {
                                builder.Append('\t');
                            }

                            builder.Append(arguments.AsStringUsingMeta(context, i, "print"));
                        }

                        outputLines.Add(builder.ToString());
                        return DynValue.Nil;
                    }
                )
            );
            script.DoString(chunk ?? string.Empty, null, "stdin-helper");
            outputLines.RemoveAll(line =>
                line.StartsWith("stdin", StringComparison.OrdinalIgnoreCase)
            );

            return outputLines;
        }

        private static Queue<string> BuildDebugInputQueue(string stdinText)
        {
            Queue<string> queue = new();

            if (string.IsNullOrEmpty(stdinText))
            {
                return queue;
            }

            string normalized = stdinText
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');
            string[] segments = normalized.Split('\n');

            for (int index = 0; index < segments.Length; index++)
            {
                string segment = segments[index];

                if (index == segments.Length - 1 && segment.Length == 0)
                {
                    continue;
                }

                queue.Enqueue(segment);
            }

            return queue;
        }

        private sealed class TapStdinHelperContext
        {
            public TapStdinHelperContext(
                LuaCompatibilityVersion compatibilityVersion,
                string testDirectory
            )
            {
                CompatibilityVersion = compatibilityVersion;
                WorkingDirectory = Environment.CurrentDirectory ?? string.Empty;
                BaseDirectory = AppContext.BaseDirectory ?? string.Empty;
                TestDirectory = testDirectory ?? string.Empty;
            }

            public LuaCompatibilityVersion CompatibilityVersion { get; }

            public string WorkingDirectory { get; }

            public string BaseDirectory { get; }

            public string TestDirectory { get; }

            public string ResolveInputPath(string relativePath)
            {
                string runtimeDirectory = Environment.CurrentDirectory ?? string.Empty;
                return ResolveInputPathCore(
                    relativePath,
                    WorkingDirectory,
                    TestDirectory,
                    BaseDirectory,
                    runtimeDirectory
                );
            }
        }

        private static string ResolveInputPathCore(
            string relativePath,
            string workingDirectory,
            string testDirectory,
            string baseDirectory,
            string runtimeDirectory
        )
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ScriptRuntimeException("stdin helper requires an input file path.");
            }

            string cleaned = relativePath.Replace('/', Path.DirectorySeparatorChar);

            if (Path.IsPathRooted(cleaned))
            {
                return cleaned;
            }

            string EvaluateCandidate(string basePath)
            {
                if (string.IsNullOrEmpty(basePath))
                {
                    return null;
                }

                string path = Path.GetFullPath(cleaned, basePath);
                return File.Exists(path) ? path : null;
            }

            string candidate =
                EvaluateCandidate(workingDirectory)
                ?? EvaluateCandidate(testDirectory)
                ?? EvaluateCandidate(baseDirectory)
                ?? EvaluateCandidate(runtimeDirectory);

            if (!string.IsNullOrEmpty(candidate))
            {
                return candidate;
            }

            throw new ScriptRuntimeException(
                $"stdin helper could not locate '{Path.GetFullPath(cleaned, workingDirectory ?? string.Empty)}'."
            );
        }
    }
}
