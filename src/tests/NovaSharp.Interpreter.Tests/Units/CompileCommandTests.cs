namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Cli;
    using NovaSharp.Cli.Commands.Implementations;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CompileCommandTests
    {
        private IScriptLoader _originalLoader;
        private string _sourcePath;
        private string _targetPath;

        [SetUp]
        public void SetUp()
        {
            _originalLoader = Script.DefaultOptions.ScriptLoader;

            _sourcePath = Path.Combine(Path.GetTempPath(), $"compile_{Guid.NewGuid():N}.lua");
            _targetPath = _sourcePath + "-compiled";

            if (File.Exists(_sourcePath))
            {
                File.Delete(_sourcePath);
            }

            File.WriteAllText(_sourcePath, "-- placeholder");

            if (File.Exists(_targetPath))
            {
                File.Delete(_targetPath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Script.DefaultOptions.ScriptLoader = _originalLoader;

            if (File.Exists(_sourcePath))
            {
                File.Delete(_sourcePath);
            }

            if (File.Exists(_targetPath))
            {
                File.Delete(_targetPath);
            }
        }

        [Test]
        public void ExecuteWritesCompiledChunkToDisk()
        {
            RecordingScriptLoader loader = new() { ScriptBody = "return 123" };
            Script.DefaultOptions.ScriptLoader = loader;

            CompileCommand command = new();
            ShellContext context = new(new Script());

            command.Execute(context, _sourcePath);

            Assert.Multiple(() =>
            {
                Assert.That(loader.LastRequestedFile, Is.EqualTo(_sourcePath));
                Assert.That(loader.LoadCount, Is.EqualTo(1));
                Assert.That(File.Exists(_targetPath), Is.True);
                Assert.That(new FileInfo(_targetPath).Length, Is.GreaterThan(0));
            });
        }

        [Test]
        public void ExecuteWhenLoaderThrowsLeavesNoArtefact()
        {
            Script.DefaultOptions.ScriptLoader = new ThrowingScriptLoader();
            CompileCommand command = new();
            ShellContext context = new(new Script());

            using StringWriter writer = new();
            TextWriter originalOut = Console.Out;
            Console.SetOut(writer);
            try
            {
                command.Execute(context, _sourcePath);
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(_targetPath), Is.False);
                Assert.That(writer.ToString(), Does.Contain($"Failed to compile '{_sourcePath}'"));
            });
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
