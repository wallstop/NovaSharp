namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using Facade = global::NovaSharp;

    /// <summary>
    /// Measures B0 facade execution overhead against the underlying Script APIs it forwards to.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class RuntimeBenchmarksB0FacadeRunOverhead
    {
        private const string RunSource = "return 42";
        private const string RunChunkName = "b0_facade_run";

        private Script _script;
        private Facade.LuaEngine _engine;

        /// <summary>
        /// Prepares matching current-API and facade engines before each benchmark run.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _engine = Facade.LuaEngine.Create(
                new Facade.LuaEngineOptions
                {
                    Modules = (Facade.LuaCoreModules)CoreModulePresets.Complete,
                    EnableScriptCaching = true,
                }
            );

            _script.DoString(RunSource, null, RunChunkName);
            _engine.Run(RunSource, RunChunkName);
        }

        /// <summary>
        /// Releases the facade engine after BenchmarkDotNet finishes this benchmark case.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _engine?.Dispose();
        }

        /// <summary>
        /// Executes a cached chunk through the current Script API.
        /// </summary>
        [Benchmark(Baseline = true, Description = "Script.DoString Cached")]
        public DynValue ScriptDoStringCached()
        {
            return _script.DoString(RunSource, null, RunChunkName);
        }

        /// <summary>
        /// Executes a cached chunk through the B0 LuaEngine facade.
        /// </summary>
        [Benchmark(Description = "LuaEngine.Run Cached")]
        public Facade.LuaValue LuaEngineRunCached()
        {
            return _engine.Run(RunSource, RunChunkName);
        }
    }

    /// <summary>
    /// Measures B0 facade call overhead across the fixed-arity call ladder.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class RuntimeBenchmarksB0FacadeCallOverhead
    {
        private Script _script;
        private DynValue _scriptFunction = DynValue.Nil;
        private Facade.LuaEngine _engine;
        private Facade.LuaFunction _facadeFunction;
        private DynValue _first = DynValue.Nil;
        private DynValue _second = DynValue.Nil;
        private DynValue _third = DynValue.Nil;
        private Facade.LuaValue _facadeFirst;
        private Facade.LuaValue _facadeSecond;
        private Facade.LuaValue _facadeThird;

        /// <summary>
        /// Fixed call arity exercised for the current benchmark case.
        /// </summary>
        [Params(0, 1, 2, 3)]
        public int Arity { get; set; }

        /// <summary>
        /// Prepares matching current-API and facade functions before each benchmark run.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            string source = GetFunctionSource(Arity);
            _script = new Script(CoreModulePresets.Complete);
            _scriptFunction = _script.DoString(source);
            _engine = Facade.LuaEngine.Create(
                new Facade.LuaEngineOptions
                {
                    Modules = (Facade.LuaCoreModules)CoreModulePresets.Complete,
                    EnableScriptCaching = true,
                }
            );
            _facadeFunction = _engine.Run(source).AsFunction();
            _first = DynValue.FromNumber(1);
            _second = DynValue.FromNumber(2);
            _third = DynValue.FromNumber(3);
            _facadeFirst = Facade.LuaValue.FromNumber(1);
            _facadeSecond = Facade.LuaValue.FromNumber(2);
            _facadeThird = Facade.LuaValue.FromNumber(3);
        }

        /// <summary>
        /// Releases the facade engine after BenchmarkDotNet finishes this benchmark case.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _engine?.Dispose();
        }

        /// <summary>
        /// Calls a cached Lua function through the current Script API.
        /// </summary>
        [Benchmark(Baseline = true, Description = "Script.Call fixed arity")]
        public DynValue ScriptCallFixedArity()
        {
            switch (Arity)
            {
                case 0:
                    return _script.Call(_scriptFunction);
                case 1:
                    return _script.Call(_scriptFunction, _first);
                case 2:
                    return _script.Call(_scriptFunction, _first, _second);
                case 3:
                    return _script.Call(_scriptFunction, _first, _second, _third);
                default:
                    throw new InvalidOperationException("Unsupported facade benchmark arity.");
            }
        }

        /// <summary>
        /// Calls a cached Lua function through the B0 LuaEngine facade.
        /// </summary>
        [Benchmark(Description = "LuaEngine.Call fixed arity")]
        public Facade.LuaValue LuaEngineCallFixedArity()
        {
            switch (Arity)
            {
                case 0:
                    return _engine.Call(_facadeFunction);
                case 1:
                    return _engine.Call(_facadeFunction, _facadeFirst);
                case 2:
                    return _engine.Call(_facadeFunction, _facadeFirst, _facadeSecond);
                case 3:
                    return _engine.Call(_facadeFunction, _facadeFirst, _facadeSecond, _facadeThird);
                default:
                    throw new InvalidOperationException("Unsupported facade benchmark arity.");
            }
        }

        /// <summary>
        /// Calls a cached Lua function through the B0 LuaFunction convenience wrapper.
        /// </summary>
        [Benchmark(Description = "LuaFunction.Call fixed arity")]
        public Facade.LuaValue LuaFunctionCallFixedArity()
        {
            switch (Arity)
            {
                case 0:
                    return _facadeFunction.Call();
                case 1:
                    return _facadeFunction.Call(_facadeFirst);
                case 2:
                    return _facadeFunction.Call(_facadeFirst, _facadeSecond);
                case 3:
                    return _facadeFunction.Call(_facadeFirst, _facadeSecond, _facadeThird);
                default:
                    throw new InvalidOperationException("Unsupported facade benchmark arity.");
            }
        }

        private static string GetFunctionSource(int arity)
        {
            switch (arity)
            {
                case 0:
                    return "return function() return 42 end";
                case 1:
                    return "return function(a) return a end";
                case 2:
                    return "return function(a, b) return b end";
                case 3:
                    return "return function(a, b, c) return c end";
                default:
                    throw new ArgumentOutOfRangeException(nameof(arity));
            }
        }
    }
}
