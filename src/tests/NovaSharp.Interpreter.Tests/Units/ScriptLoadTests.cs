namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Infrastructure;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptLoadTests
    {
        [Test]
        public void LoadStringDecodesBase64Dump()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return 77");

            string encodedDump = EncodeFunctionAsBase64(script, chunk);
            DynValue loaded = script.LoadString(encodedDump);

            Assert.That(script.Call(loaded).Number, Is.EqualTo(77));
        }

        [Test]
        public void LoadStreamExecutesTextWhenStreamIsNotDump()
        {
            Script script = new();
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("return 13"));

            DynValue function = script.LoadStream(stream);

            Assert.That(script.Call(function).Number, Is.EqualTo(13));
        }

        [Test]
        public void LoadStreamHandlesShortTextStreams()
        {
            Script script = new();
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("x=41"));

            DynValue chunk = script.LoadStream(stream);
            script.Call(chunk);

            Assert.That(script.Globals.Get("x").Number, Is.EqualTo(41));
        }

        [Test]
        public void LoadStreamUndumpsBinaryChunk()
        {
            Script script = new();
            byte[] dump = DumpToBytes(script, script.LoadString("return 19"));
            using MemoryStream stream = new(dump, writable: false);

            DynValue function = script.LoadStream(stream);

            Assert.That(script.Call(function).Number, Is.EqualTo(19));
        }

        [Test]
        public void LoadFunctionBindsProvidedEnvironment()
        {
            Script script = new();
            Table env = new(script);
            env.Set("value", DynValue.NewNumber(7));

            DynValue closure = script.LoadFunction("return value", env, "bound");
            DynValue result = script.Call(closure);

            Assert.That(result.Number, Is.EqualTo(7));
            Assert.That(script.Globals.Get("value").IsNil(), Is.True);
        }

        [Test]
        public void LoadStreamUndumpPreservesEnvironmentUpValues()
        {
            Script producer = new();
            producer.Globals.Set("shared", DynValue.NewNumber(5));
            DynValue chunk = producer.LoadString(
                @"
                return function()
                    return shared
                end
            "
            );
            byte[] dump = DumpToBytes(producer, chunk);

            Script consumer = new();
            consumer.Globals.Set("shared", DynValue.NewNumber(10));

            using MemoryStream stream = new(dump, writable: false);
            DynValue chunkResult = consumer.LoadStream(stream);
            DynValue restoredFunction = consumer.Call(chunkResult);
            DynValue result = consumer.Call(restoredFunction);

            Assert.That(result.Number, Is.EqualTo(10));
        }

        [Test]
        public void DumpRejectsNonFunctionValues()
        {
            Script script = new();
            using MemoryStream stream = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(DynValue.NewNumber(1), stream)
            )!;

            Assert.That(exception.Message, Does.Contain("function arg is not a function"));
        }

        [Test]
        public void DumpRejectsReadOnlyStreams()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return 1");
            using MemoryStream stream = new(Array.Empty<byte>(), writable: false);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(chunk, stream)
            )!;

            Assert.That(exception.Message, Does.Contain("stream is readonly"));
        }

        [Test]
        public void DumpRejectsFunctionsWithExternalUpValues()
        {
            Script script = new();
            DynValue chunk = script.LoadString(
                @"
                local capture = 5
                return function() return capture end
                "
            );
            DynValue closure = script.Call(chunk);

            using MemoryStream stream = new();
            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Dump(closure, stream)
            )!;

            Assert.That(exception.Message, Does.Contain("upvalues other than _ENV"));
        }

        [Test]
        public void DumpThrowsWhenFunctionIsNull()
        {
            Script script = new();
            using MemoryStream stream = new();

            Assert.That(
                () => script.Dump(null, stream),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("function")
            );
        }

        [Test]
        public void DumpThrowsWhenStreamIsNull()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return 1");

            Assert.That(
                () => script.Dump(chunk, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("stream")
            );
        }

        [Test]
        public void LoadFileExecutesStringsBytesAndStreamsFromLoader()
        {
            Script script = new();
            StubScriptLoader loader = new() { Mode = LoaderMode.String, Source = "return 21" };
            script.Options.ScriptLoader = loader;

            DynValue fromString = script.LoadFile("string.lua");
            Assert.That(script.Call(fromString).Number, Is.EqualTo(21));

            loader.Mode = LoaderMode.Bytes;
            loader.Source = "return 22";
            DynValue fromBytes = script.LoadFile("bytes.lua");
            Assert.That(script.Call(fromBytes).Number, Is.EqualTo(22));

            loader.Mode = LoaderMode.Stream;
            loader.Source = "return 23";
            DynValue fromStream = script.LoadFile("stream.lua");
            Assert.Multiple(() =>
            {
                Assert.That(script.Call(fromStream).Number, Is.EqualTo(23));
                Assert.That(loader.StreamDisposed, Is.True);
            });
        }

        [Test]
        public void LoadFileThrowsOnNullAndUnsupportedResults()
        {
            Script script = new();
            StubScriptLoader loader = new() { Mode = LoaderMode.Null };
            script.Options.ScriptLoader = loader;

            InvalidCastException nullException = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("broken.lua")
            )!;
            Assert.That(nullException.Message, Does.Contain("Unexpected null"));

            loader.Mode = LoaderMode.Unsupported;
            InvalidCastException unsupportedException = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("still-broken.lua")
            )!;
            Assert.That(unsupportedException.Message, Does.Contain("Unsupported return type"));
        }

        [Test]
        public void ScriptUsesCustomTimeProviderForStartTimestamp()
        {
            FrozenTimeProvider provider = new(
                DateTimeOffset.Parse("2025-11-16T10:00:00Z", CultureInfo.InvariantCulture)
            );
            ScriptOptions options = new(Script.DefaultOptions) { TimeProvider = provider };
            Script script = new(options);

            Assert.Multiple(() =>
            {
                Assert.That(script.TimeProvider, Is.SameAs(provider));
                Assert.That(script.StartTimeUtc, Is.EqualTo(provider.GetUtcNow().UtcDateTime));
            });
        }

        [Test]
        public void CallInvokesMetamethodWhenValueIsCallable()
        {
            Script script = new();
            DynValue callableTable = script.DoString(
                @"
                local t = {}
                setmetatable(t, { __call = function(_, value) return value * 2 end })
                return t
                "
            );

            DynValue result = script.Call(callableTable, DynValue.NewNumber(6));

            Assert.That(result.Number, Is.EqualTo(12));
        }

        [Test]
        public void CallExecutesClrFunctionCallbacks()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback(
                (_, args) => DynValue.NewNumber(args[0].Number + 5)
            );

            DynValue result = script.Call(callback, DynValue.NewNumber(7));

            Assert.That(result.Number, Is.EqualTo(12));
        }

        [Test]
        public void CallConvertsObjectArgumentsToDynValues()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return function(value) return value * 3 end");
            DynValue multiplier = script.Call(chunk);

            DynValue result = script.Call(multiplier, 4);

            Assert.That(result.Number, Is.EqualTo(12));
        }

        [Test]
        public void CallComplainsWhenValueIsNotCallable()
        {
            Script script = new();

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                script.Call(DynValue.NewString("nope"))
            )!;

            Assert.That(exception.Message, Does.Contain("has no __call metamethod"));
        }

        [Test]
        public void CreateCoroutineSupportsClrFunctionsAndValidatesInputs()
        {
            Script script = new();
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewString("done"));

            DynValue coroutine = script.CreateCoroutine(callback);
            Assert.That(coroutine.Type, Is.EqualTo(DataType.Thread));

            Assert.Throws<ArgumentException>(() => script.CreateCoroutine(DynValue.NewNumber(1)));
        }

        [Test]
        public void RunStringAndRunFileExecuteConvenienceHelpers()
        {
            DynValue stringResult = Script.RunString("return 321");
            Assert.That(stringResult.Number, Is.EqualTo(321));

            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllText(path, "return 654");
                DynValue fileResult = Script.RunFile(path);
                Assert.That(fileResult.Number, Is.EqualTo(654));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void RecycleCoroutineProducesFreshThread()
        {
            Script script = new();
            DynValue source = CompileFunction(
                script,
                "function(value) coroutine.yield(value + 1); return value + 2 end"
            );
            Coroutine coroutine = script.CreateCoroutine(source).Coroutine;

            Assert.That(coroutine.Resume(DynValue.NewNumber(3)).Number, Is.EqualTo(4));
            Assert.That(coroutine.Resume().Number, Is.EqualTo(5));
            Assert.That(coroutine.State, Is.EqualTo(CoroutineState.Dead));

            DynValue replacement = CompileFunction(script, "function() return 99 end");
            DynValue recycled = script.RecycleCoroutine(coroutine, replacement);

            Assert.Multiple(() =>
            {
                Assert.That(recycled.Type, Is.EqualTo(DataType.Thread));
                Assert.That(recycled.Coroutine.State, Is.EqualTo(CoroutineState.NotStarted));
                Assert.That(recycled.Coroutine.Resume().Number, Is.EqualTo(99));
            });
        }

        [Test]
        public void RecycleCoroutineRequiresDeadCoroutineAndFunction()
        {
            Script script = new();
            DynValue worker = CompileFunction(
                script,
                "function() coroutine.yield(1); return 2 end"
            );
            Coroutine live = script.CreateCoroutine(worker).Coroutine;

            Assert.Throws<InvalidOperationException>(() => script.RecycleCoroutine(live, worker));

            live.Resume();
            live.Resume();
            Assert.That(live.State, Is.EqualTo(CoroutineState.Dead));

            Assert.Throws<InvalidOperationException>(() =>
                script.RecycleCoroutine(live, DynValue.NewNumber(1))
            );
        }

        [Test]
        public void RecycleCoroutineRequiresCoroutineInstance()
        {
            Script script = new();
            DynValue worker = CompileFunction(script, "function() return 0 end");

            Assert.Throws<InvalidOperationException>(() => script.RecycleCoroutine(null, worker));
        }

        [Test]
        public void DoStreamExecutesTextStreams()
        {
            Script script = new();
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("return 123"));

            DynValue result = script.DoStream(stream);

            Assert.That(result.Number, Is.EqualTo(123));
        }

        private static readonly string[] AnswerModuleName = { "answer" };
        private static readonly string[] AnswerModuleFile = { "answer.lua" };

        [Test]
        public void RequireModuleLoadsChunkViaScriptLoader()
        {
            ModuleScriptLoader loader = new() { ModuleCode = "return 42" };
            Script script = new(new ScriptOptions { ScriptLoader = loader });

            DynValue module = script.RequireModule("answer");
            DynValue result = script.Call(module);

            Assert.Multiple(() =>
            {
                Assert.That(loader.ResolvedModuleNames, Is.EqualTo(AnswerModuleName));
                Assert.That(loader.LoadedFiles, Is.EqualTo(AnswerModuleFile));
                Assert.That(result.Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void RequireModuleThrowsWhenLoaderCannotResolveName()
        {
            ModuleScriptLoader loader = new() { ModuleExists = false };
            Script script = new(new ScriptOptions { ScriptLoader = loader });

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.RequireModule("missing")
            )!;

            Assert.That(exception.Message, Does.Contain("module 'missing' not found"));
        }

        private static DynValue CompileFunction(Script script, string luaFunctionSource)
        {
            DynValue chunk = script.LoadString($"return {luaFunctionSource}");
            return script.Call(chunk);
        }

        private static string EncodeFunctionAsBase64(Script script, DynValue chunk)
        {
            using MemoryStream stream = new();
            script.Dump(chunk, stream);
            return StringModule.Base64DumpHeader + Convert.ToBase64String(stream.ToArray());
        }

        private static byte[] DumpToBytes(Script script, DynValue chunk)
        {
            using MemoryStream stream = new();
            script.Dump(chunk, stream);
            return stream.ToArray();
        }

        private sealed class FrozenTimeProvider : ITimeProvider
        {
            private readonly DateTimeOffset _timestamp;

            public FrozenTimeProvider(DateTimeOffset timestamp)
            {
                _timestamp = timestamp;
            }

            public DateTimeOffset GetUtcNow()
            {
                return _timestamp;
            }
        }

        private enum LoaderMode
        {
            String,
            Bytes,
            Stream,
            Null,
            Unsupported,
        }

        private sealed class StubScriptLoader : IScriptLoader
        {
            public LoaderMode Mode { get; set; }

            public string Source { get; set; } = "return 0";

            public bool StreamDisposed { get; private set; }

            public object LoadFile(string file, Table globalContext)
            {
                return Mode switch
                {
                    LoaderMode.String => Source,
                    LoaderMode.Bytes => Encoding.UTF8.GetBytes(Source),
                    LoaderMode.Stream => new TrackingStream(
                        Encoding.UTF8.GetBytes(Source),
                        () => StreamDisposed = true
                    ),
                    LoaderMode.Null => null,
                    LoaderMode.Unsupported => new object(),
                    _ => throw new InvalidOperationException(),
                };
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }

            private sealed class TrackingStream : MemoryStream
            {
                private readonly Action _onDispose;

                public TrackingStream(byte[] buffer, Action onDispose)
                    : base(buffer, writable: false)
                {
                    _onDispose = onDispose;
                }

                protected override void Dispose(bool disposing)
                {
                    if (disposing)
                    {
                        _onDispose();
                    }

                    base.Dispose(disposing);
                }
            }
        }

        private sealed class ModuleScriptLoader : IScriptLoader
        {
            private readonly List<string> _resolved = new();
            private readonly List<string> _loaded = new();

            public bool ModuleExists { get; set; } = true;

            public string ModuleCode { get; set; } = "return function() return 0 end";

            public IReadOnlyList<string> ResolvedModuleNames => _resolved;

            public IReadOnlyList<string> LoadedFiles => _loaded;

            public object LoadFile(string file, Table globalContext)
            {
                _loaded.Add(file);
                return ModuleCode;
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                _resolved.Add(modname);
                return ModuleExists ? $"{modname}.lua" : null;
            }
        }
    }
}
