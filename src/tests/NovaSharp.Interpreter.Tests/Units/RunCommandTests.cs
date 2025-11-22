namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RunCommandTests
    {
        private TextWriter _originalOut = null!;
        private StringWriter _writer = null!;

        [SetUp]
        public void SetUp()
        {
            _writer = new StringWriter();
            _originalOut = Console.Out;
            Console.SetOut(_writer);
        }

        [TearDown]
        public void TearDown()
        {
            Console.SetOut(_originalOut);
            _writer.Dispose();
        }

        [Test]
        public void ExecuteWithoutArgumentsWritesSyntaxHint()
        {
            RunCommand command = new();
            ShellContext context = new(new Script());

            command.Execute(context, string.Empty);

            Assert.That(_writer.ToString(), Does.Contain("Syntax : !run <file>"));
        }

        [Test]
        public void ExecuteLoadsScriptThroughConfiguredLoader()
        {
            RecordingScriptLoader loader = new();
            Script script = new()
            {
                Globals = { ["marker"] = DynValue.NewNumber(0) },
                Options = { ScriptLoader = loader },
            };

            loader.ScriptBody = "marker = (marker or 0) + 1";

            RunCommand command = new();
            ShellContext context = new(script);

            command.Execute(context, "sample.lua");

            Assert.Multiple(() =>
            {
                Assert.That(loader.LastRequestedFile, Is.EqualTo("sample.lua"));
                Assert.That(loader.LoadCount, Is.EqualTo(1));
                Assert.That(script.Globals.Get("marker").Number, Is.EqualTo(1));
            });
        }

        [Test]
        public void ExecuteWithMissingFilePropagatesLoaderException()
        {
            ThrowingScriptLoader loader = new();
            Script script = new() { Options = { ScriptLoader = loader } };
            RunCommand command = new();

            Assert.That(
                () => command.Execute(new ShellContext(script), "missing.lua"),
                Throws.TypeOf<FileNotFoundException>()
            );
        }

        [Test]
        public void ExecuteWithoutManifestLogsCompatibilitySummary()
        {
            RecordingScriptLoader loader = new();
            Script script = new()
            {
                Options =
                {
                    ScriptLoader = loader,
                    CompatibilityVersion = LuaCompatibilityVersion.Lua54,
                },
            };

            RunCommand command = new();
            ShellContext context = new(script);

            command.Execute(context, "sample.lua");

            Assert.That(
                _writer.ToString(),
                Does.Contain("[compatibility] Running").And.Contain("Lua 5.4")
            );
        }

        [Test]
        public void ExecuteWithManifestRunsScriptInCompatibilityInstance()
        {
            string modDirectory = Path.Combine(Path.GetTempPath(), $"mod_{Guid.NewGuid():N}");
            Directory.CreateDirectory(modDirectory);

            string scriptPath = Path.Combine(modDirectory, "entry.lua");
            File.WriteAllText(
                scriptPath,
                "if warn ~= nil then error('warn available') end\ncontextFlag = true\n"
            );

            string manifestPath = Path.Combine(modDirectory, "mod.json");
            File.WriteAllText(
                manifestPath,
                "{\n"
                    + "    \"name\": \"CompatMod\",\n"
                    + "    \"luaCompatibility\": \"Lua53\"\n"
                    + "}\n"
            );

            RunCommand command = new();
            Script script = new();
            ShellContext context = new(script);

            try
            {
                command.Execute(context, scriptPath);

                Assert.Multiple(() =>
                {
                    Assert.That(
                        _writer.ToString(),
                        Does.Contain("[compatibility] Applied Lua 5.3 profile")
                            .And.Contain("Lua 5.3")
                            .And.Contain("[compatibility] Running")
                    );
                    Assert.That(script.Globals.Get("contextFlag").IsNil());
                });
            }
            finally
            {
                if (Directory.Exists(modDirectory))
                {
                    Directory.Delete(modDirectory, recursive: true);
                }
            }
        }

        private sealed class RecordingScriptLoader : IScriptLoader
        {
            public string ScriptBody { get; set; } = "return";

            public string LastRequestedFile { get; private set; } = string.Empty;

            public int LoadCount { get; private set; }

            public object LoadFile(string file, Table globalContext)
            {
                LastRequestedFile = file;
                LoadCount++;
                return ScriptBody;
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }

        private sealed class ThrowingScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                throw new FileNotFoundException($"Missing {file}");
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }
    }
}
