namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using global::NovaSharp;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modding;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// BenchmarkDotNet suite that executes representative NovaSharp runtime scenarios.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class RuntimeBenchmarks
    {
        private Script _script;
        private LuaValue _compiledEntry = LuaValue.Nil;
        private Func<double> _scenarioRunner;
        private BenchmarkHost _host = new();

        /// <summary>
        /// Scenario that will be executed for the next benchmark iteration.
        /// </summary>
        [Params(
            nameof(RuntimeScenario.NumericLoops),
            nameof(RuntimeScenario.TableMutation),
            nameof(RuntimeScenario.CoroutinePipeline),
            nameof(RuntimeScenario.UserDataInterop)
        )]
        public string ScenarioName { get; set; } = nameof(RuntimeScenario.NumericLoops);

        private RuntimeScenario CurrentScenario
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(ScenarioName);
                return Enum.Parse<RuntimeScenario>(ScenarioName, ignoreCase: false);
            }
        }

        [GlobalSetup]
        /// <summary>
        /// Compiles the scenario script and prepares the helpers before the benchmark run.
        /// </summary>
        public void Setup()
        {
            RuntimeScenario scenario = CurrentScenario;

            _script = new Script(CoreModulePresets.Complete);
            _compiledEntry = _script.LoadString(
                LuaRuntimeSuites.GetScript(scenario),
                null,
                $"scenario_{scenario}"
            );

            _scenarioRunner = scenario switch
            {
                RuntimeScenario.NumericLoops => () => _script.CallValues(_compiledEntry).Number,
                RuntimeScenario.TableMutation => RunTableScenario,
                RuntimeScenario.CoroutinePipeline => RunCoroutineScenario,
                RuntimeScenario.UserDataInterop => RunUserDataScenario,
                _ => () => _script.CallValues(_compiledEntry).Number,
            };

            if (
                scenario == RuntimeScenario.UserDataInterop
                && !UserData.IsTypeRegistered<BenchmarkHost>()
            )
            {
                UserData.RegisterType<BenchmarkHost>();
            }

            _host = new BenchmarkHost();
        }

        /// <summary>
        /// Executes the selected scenario and returns its numeric result.
        /// </summary>
        [Benchmark(Description = "Scenario Execution")]
        public double ExecuteScenario() => _scenarioRunner!();

        private double RunTableScenario()
        {
            Table table = new(_script);
            for (int i = 1; i <= LuaRuntimeSuites.TableEntryCount; i++)
            {
                table.Set(i, LuaValue.NewNumber(i * 1.5));
            }

            return _script.CallObjectArgumentsCore(_compiledEntry, table).Number;
        }

        private double RunCoroutineScenario() =>
            _script.CallValues(_compiledEntry, LuaRuntimeSuites.CoroutineSteps).Number;

        private double RunUserDataScenario()
        {
            _host.Reset();
            return _script
                .CallObjectArgumentsCore(_compiledEntry, _host, LuaRuntimeSuites.UserDataIterations)
                .Number;
        }
    }

    /// <summary>
    /// Host object exposed to Lua scripts for the userdata interop scenario.
    /// </summary>
    internal sealed class BenchmarkHost
    {
        private double _store;

        /// <summary>
        /// Sums the provided operands and caches the intermediate result.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Computed value used inside the scenario.</returns>
        public double Accumulate(double left, double right)
        {
            double result = (left * 1.25) + (right * 0.75);
            _store = result;
            return result;
        }

        /// <summary>
        /// Persists the supplied value in the backing store.
        /// </summary>
        public void Store(double value) => _store = value;

        /// <summary>
        /// Gets the most recently stored value.
        /// </summary>
        public double Stored => _store;

        /// <summary>
        /// Resets the backing store to zero.
        /// </summary>
        public void Reset() => _store = 0;
    }

    /// <summary>
    /// Benchmarks host-to-Lua calls through fixed LuaValue public API overloads.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class HostCallBenchmarks
    {
        private Script _script;
        private LuaValue _oneArgFunction = LuaValue.Nil;
        private LuaValue _twoArgFunction = LuaValue.Nil;
        private LuaValue _threeArgFunction = LuaValue.Nil;
        private LuaValue _fourArgFunction = LuaValue.Nil;
        private LuaValue _fiveArgFunction = LuaValue.Nil;
        private LuaValue _sixArgFunction = LuaValue.Nil;
        private LuaValue _sevenArgFunction = LuaValue.Nil;
        private LuaValue _callableLuaTable = LuaValue.Nil;
        private LuaValue _callableCallbackViewTable = LuaValue.Nil;
        private LuaValue _callableNoContextCallbackViewTable = LuaValue.Nil;
        private LuaValue _coroutineFunction = LuaValue.Nil;
        private LuaValue _fourArgCoroutineFunction = LuaValue.Nil;
        private LuaValue _fiveArgCoroutineFunction = LuaValue.Nil;
        private LuaValue _sixArgCoroutineFunction = LuaValue.Nil;
        private LuaValue _sevenArgCoroutineFunction = LuaValue.Nil;
        private Closure _threeArgClosure;
        private Closure _fourArgClosure;
        private Closure _fiveArgClosure;
        private Closure _sixArgClosure;
        private Closure _sevenArgClosure;
        private Coroutine _runningCoroutine;
        private Coroutine _fourArgRunningCoroutine;
        private Coroutine _fiveArgRunningCoroutine;
        private Coroutine _sixArgRunningCoroutine;
        private Coroutine _sevenArgRunningCoroutine;
        private LuaValue _first = LuaValue.Nil;
        private LuaValue _second = LuaValue.Nil;
        private LuaValue _third = LuaValue.Nil;
        private LuaValue _fourth = LuaValue.Nil;
        private LuaValue _fifth = LuaValue.Nil;
        private LuaValue _sixth = LuaValue.Nil;
        private LuaValue _seventh = LuaValue.Nil;
        private LuaValue[] _fiveDynValueArgs = Array.Empty<LuaValue>();
        private LuaValue[] _sixDynValueArgs = Array.Empty<LuaValue>();
        private LuaValue[] _sevenDynValueArgs = Array.Empty<LuaValue>();
        private LuaValue[] _fiveDynValueArgsWithPadding = Array.Empty<LuaValue>();
        private object _firstObject = 1d;
        private object _secondObject = 2d;
        private object _thirdObject = 3d;
        private object _fourthObject = 4d;
        private object _fifthObject = 5d;
        private object _sixthObject = 6d;
        private object _seventhObject = 7d;
        private object[] _fiveObjectArgs = Array.Empty<object>();
        private object[] _sixObjectArgs = Array.Empty<object>();
        private object[] _sevenObjectArgs = Array.Empty<object>();
        private object[] _fiveObjectArgsWithPadding = Array.Empty<object>();

        [GlobalSetup]
        /// <summary>
        /// Compiles tiny identity functions and prepares stable argument values.
        /// </summary>
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _oneArgFunction = _script.DoString("return function(a) return a end");
            _twoArgFunction = _script.DoString("return function(a, b) return b end");
            _threeArgFunction = _script.DoString("return function(a, b, c) return c end");
            _fourArgFunction = _script.DoString("return function(a, b, c, d) return d end");
            _fiveArgFunction = _script.DoString("return function(a, b, c, d, e) return e end");
            _sixArgFunction = _script.DoString("return function(a, b, c, d, e, f) return f end");
            _sevenArgFunction = _script.DoString(
                "return function(a, b, c, d, e, f, g) return g end"
            );
            _threeArgClosure = _threeArgFunction.Function;
            _fourArgClosure = _fourArgFunction.Function;
            _fiveArgClosure = _fiveArgFunction.Function;
            _sixArgClosure = _sixArgFunction.Function;
            _sevenArgClosure = _sevenArgFunction.Function;
            _callableLuaTable = _script.DoString(
                "return setmetatable({}, { __call = function(_, a, b, c, d, e) return e end })"
            );
            Table callableCallbackTable = new(_script);
            Table callableCallbackMeta = new(_script);
            callableCallbackMeta.Set("__call", LuaValue.NewCallbackView((_, args) => args[5]));
            callableCallbackTable.MetaTable = callableCallbackMeta;
            _callableCallbackViewTable = LuaValue.NewTable(callableCallbackTable);
            Table callableNoContextCallbackTable = new(_script);
            Table callableNoContextCallbackMeta = new(_script);
            callableNoContextCallbackMeta.Set(
                "__call",
                LuaValue.NewCallbackView((CallbackArgumentsView args) => args[5])
            );
            callableNoContextCallbackTable.MetaTable = callableNoContextCallbackMeta;
            _callableNoContextCallbackViewTable = LuaValue.NewTable(callableNoContextCallbackTable);
            _coroutineFunction = _script.DoString(
                "return function(a, b, c) while true do a, b, c = coroutine.yield(c) end end"
            );
            _fourArgCoroutineFunction = _script.DoString(
                "return function(a, b, c, d) while true do a, b, c, d = coroutine.yield(d) end end"
            );
            _fiveArgCoroutineFunction = _script.DoString(
                "return function(a, b, c, d, e) while true do a, b, c, d, e = coroutine.yield(e) end end"
            );
            _sixArgCoroutineFunction = _script.DoString(
                "return function(a, b, c, d, e, f) while true do a, b, c, d, e, f = coroutine.yield(f) end end"
            );
            _sevenArgCoroutineFunction = _script.DoString(
                "return function(a, b, c, d, e, f, g) while true do a, b, c, d, e, f, g = coroutine.yield(g) end end"
            );
            _first = LuaValue.NewNumber(1d);
            _second = LuaValue.NewNumber(2d);
            _third = LuaValue.NewNumber(3d);
            _fourth = LuaValue.NewNumber(4d);
            _fifth = LuaValue.NewNumber(5d);
            _sixth = LuaValue.NewNumber(6d);
            _seventh = LuaValue.NewNumber(7d);
            _fiveDynValueArgs = new[] { _first, _second, _third, _fourth, _fifth };
            _sixDynValueArgs = new[] { _first, _second, _third, _fourth, _fifth, _sixth };
            _sevenDynValueArgs = new[]
            {
                _first,
                _second,
                _third,
                _fourth,
                _fifth,
                _sixth,
                _seventh,
            };
            _fiveDynValueArgsWithPadding = new[]
            {
                LuaValue.Nil,
                _first,
                _second,
                _third,
                _fourth,
                _fifth,
                LuaValue.Nil,
            };
            _firstObject = 1d;
            _secondObject = 2d;
            _thirdObject = 3d;
            _fourthObject = 4d;
            _fifthObject = 5d;
            _sixthObject = 6d;
            _seventhObject = 7d;
            _fiveObjectArgs = new[]
            {
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
            };
            _sixObjectArgs = new[]
            {
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                _sixthObject,
            };
            _sevenObjectArgs = new[]
            {
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                _sixthObject,
                _seventhObject,
            };
            _fiveObjectArgsWithPadding = new[]
            {
                0d,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                0d,
            };
            _runningCoroutine = _script.CreateCoroutineValue(_coroutineFunction).Coroutine;
            _runningCoroutine.ResumeValues(_first, _second, _third);
            _fourArgRunningCoroutine = _script
                .CreateCoroutineValue(_fourArgCoroutineFunction)
                .Coroutine;
            _fourArgRunningCoroutine.ResumeValues(_first, _second, _third, _fourth);
            _fiveArgRunningCoroutine = _script
                .CreateCoroutineValue(_fiveArgCoroutineFunction)
                .Coroutine;
            _fiveArgRunningCoroutine.ResumeValues(_first, _second, _third, _fourth, _fifth);
            _sixArgRunningCoroutine = _script
                .CreateCoroutineValue(_sixArgCoroutineFunction)
                .Coroutine;
            _sixArgRunningCoroutine.ResumeValues(_first, _second, _third, _fourth, _fifth, _sixth);
            _sevenArgRunningCoroutine = _script
                .CreateCoroutineValue(_sevenArgCoroutineFunction)
                .Coroutine;
            _sevenArgRunningCoroutine.ResumeValues(
                _first,
                _second,
                _third,
                _fourth,
                _fifth,
                _sixth,
                _seventh
            );
        }

        /// <summary>
        /// Calls a Lua closure with one pre-created LuaValue argument.
        /// </summary>
        [Benchmark(Description = "Host Call: 1 LuaValue")]
        public LuaValue CallOneDynValue() => _script.CallValues(_oneArgFunction, _first);

        /// <summary>
        /// Calls a Lua closure with two pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 2 DynValues")]
        public LuaValue CallTwoDynValues() => _script.CallValues(_twoArgFunction, _first, _second);

        /// <summary>
        /// Calls a Lua closure with three pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 3 DynValues")]
        public LuaValue CallThreeDynValues() =>
            _script.CallValues(_threeArgFunction, _first, _second, _third);

        /// <summary>
        /// Calls a Lua closure with four pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 4 DynValues")]
        public LuaValue CallFourDynValues() =>
            _script.CallValues(_fourArgFunction, _first, _second, _third, _fourth);

        /// <summary>
        /// Calls a Lua closure with five pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 5 DynValues")]
        public LuaValue CallFiveDynValues() =>
            _script.CallValues(_fiveArgFunction, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a Lua closure with six pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 6 DynValues")]
        public LuaValue CallSixDynValues() =>
            _script.CallValues(_sixArgFunction, _first, _second, _third, _fourth, _fifth, _sixth);

        /// <summary>
        /// Calls a Lua closure with seven pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 7 DynValues")]
        public LuaValue CallSevenDynValues() =>
            _script.CallValues(
                _sevenArgFunction,
                _first,
                _second,
                _third,
                _fourth,
                _fifth,
                _sixth,
                _seventh
            );

        /// <summary>
        /// Calls a Lua callable table with five pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: callable table 5 DynValues (Lua __call)")]
        public LuaValue CallCallableLuaTableFiveDynValues() =>
            _script.CallValues(_callableLuaTable, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a CLR callback-view callable table with five pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: callable table 5 DynValues (CLR __call)")]
        public LuaValue CallCallableCallbackViewTableFiveDynValues() =>
            _script.CallValues(
                _callableCallbackViewTable,
                _first,
                _second,
                _third,
                _fourth,
                _fifth
            );

        /// <summary>
        /// Calls a contextless CLR callback-view callable table with five pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: callable table 5 DynValues (CLR __call no context)")]
        public LuaValue CallCallableNoContextCallbackViewTableFiveDynValues() =>
            _script.CallValues(
                _callableNoContextCallbackViewTable,
                _first,
                _second,
                _third,
                _fourth,
                _fifth
            );

        /// <summary>
        /// Calls a Lua closure through the params-array overload for comparison with fixed overloads.
        /// </summary>
        [Benchmark(Description = "Host Call: params 3 DynValues")]
        public LuaValue CallThreeDynValuesParamsArray() =>
            _script.CallValues(_threeArgFunction, new LuaValue[] { _first, _second, _third });

        /// <summary>
        /// Calls a Lua closure through the params-array overload for comparison with fixed overloads.
        /// </summary>
        [Benchmark(Description = "Host Call: params 4 DynValues")]
        public LuaValue CallFourDynValuesParamsArray() =>
            _script.CallValues(
                _fourArgFunction,
                new LuaValue[] { _first, _second, _third, _fourth }
            );

        /// <summary>
        /// Calls a Lua closure through the params-array overload with five DynValues.
        /// </summary>
        [Benchmark(Description = "Host Call: params 5 DynValues")]
        public LuaValue CallFiveDynValuesParamsArray() =>
            _script.CallValues(
                _fiveArgFunction,
                new LuaValue[] { _first, _second, _third, _fourth, _fifth }
            );

        /// <summary>
        /// Calls a Lua closure through the params-array overload with six DynValues.
        /// </summary>
        [Benchmark(Description = "Host Call: params 6 DynValues")]
        public LuaValue CallSixDynValuesParamsArray() =>
            _script.CallValues(_sixArgFunction, _sixDynValueArgs);

        /// <summary>
        /// Calls a Lua closure through the params-array overload with seven DynValues.
        /// </summary>
        [Benchmark(Description = "Host Call: params 7 DynValues")]
        public LuaValue CallSevenDynValuesParamsArray() =>
            _script.CallValues(_sevenArgFunction, _sevenDynValueArgs);

        /// <summary>
        /// Calls a Lua closure with five pre-created DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "Host Call: span 5 DynValues")]
        public LuaValue CallFiveDynValuesSpan() =>
            _script.CallValues(_fiveArgFunction, _fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a Lua closure with a slice of caller-owned contiguous LuaValue storage.
        /// </summary>
        [Benchmark(Description = "Host Call: span slice 5 DynValues")]
        public LuaValue CallFiveDynValuesSpanSlice() =>
            _script.CallValues(_fiveArgFunction, _fiveDynValueArgsWithPadding.AsSpan(1, 5));

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with three pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 3 DynValues")]
        public LuaValue ClosureCallThreeDynValues() =>
            _threeArgClosure.CallValues(_first, _second, _third);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with four pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 4 DynValues")]
        public LuaValue ClosureCallFourDynValues() =>
            _fourArgClosure.CallValues(_first, _second, _third, _fourth);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with five pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 5 DynValues")]
        public LuaValue ClosureCallFiveDynValues() =>
            _fiveArgClosure.CallValues(_first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with six pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 6 DynValues")]
        public LuaValue ClosureCallSixDynValues() =>
            _sixArgClosure.CallValues(_first, _second, _third, _fourth, _fifth, _sixth);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with seven pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 7 DynValues")]
        public LuaValue ClosureCallSevenDynValues() =>
            _sevenArgClosure.CallValues(_first, _second, _third, _fourth, _fifth, _sixth, _seventh);

        /// <summary>
        /// Calls a Lua closure through the closure params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Closure Call: params 3 DynValues")]
        public LuaValue ClosureCallThreeDynValuesParamsArray() =>
            _threeArgClosure.CallValues(new LuaValue[] { _first, _second, _third });

        /// <summary>
        /// Calls a Lua closure through the closure params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Closure Call: params 4 DynValues")]
        public LuaValue ClosureCallFourDynValuesParamsArray() =>
            _fourArgClosure.CallValues(new LuaValue[] { _first, _second, _third, _fourth });

        /// <summary>
        /// Calls a Lua closure through the closure params-array overload with five DynValues.
        /// </summary>
        [Benchmark(Description = "Closure Call: params 5 DynValues")]
        public LuaValue ClosureCallFiveDynValuesParamsArray() =>
            _fiveArgClosure.CallValues(new LuaValue[] { _first, _second, _third, _fourth, _fifth });

        /// <summary>
        /// Calls a Lua closure through the closure span overload with five DynValues.
        /// </summary>
        [Benchmark(Description = "Closure Call: span 5 DynValues")]
        public LuaValue ClosureCallFiveDynValuesSpan() =>
            _fiveArgClosure.CallValues(_fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a Lua closure with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 3 objects")]
        public LuaValue CallThreeObjects() =>
            _script.CallObjectArgumentsCore(
                _threeArgFunction,
                _firstObject,
                _secondObject,
                _thirdObject
            );

        /// <summary>
        /// Calls a Lua closure with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 4 objects")]
        public LuaValue CallFourObjects() =>
            _script.CallObjectArgumentsCore(
                _fourArgFunction,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject
            );

        /// <summary>
        /// Calls a Lua closure with five pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 5 objects")]
        public LuaValue CallFiveObjects() =>
            _script.CallObjectArgumentsCore(
                _fiveArgFunction,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Calls a Lua closure with six pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 6 objects")]
        public LuaValue CallSixObjects() =>
            _script.CallObjectArgumentsCore(
                _sixArgFunction,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                _sixthObject
            );

        /// <summary>
        /// Calls a Lua closure with seven pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 7 objects")]
        public LuaValue CallSevenObjects() =>
            _script.CallObjectArgumentsCore(
                _sevenArgFunction,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                _sixthObject,
                _seventhObject
            );

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 3 objects")]
        public LuaValue ClosureCallThreeObjects() =>
            _threeArgClosure.Call(_firstObject, _secondObject, _thirdObject);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 4 objects")]
        public LuaValue ClosureCallFourObjects() =>
            _fourArgClosure.Call(_firstObject, _secondObject, _thirdObject, _fourthObject);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with five pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 5 objects")]
        public LuaValue ClosureCallFiveObjects() =>
            _fiveArgClosure.Call(
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Calls a Lua closure through the closure caller-owned object storage overload.
        /// </summary>
        [Benchmark(Description = "Closure Call: object span 5 objects")]
        public LuaValue ClosureCallFiveObjectArgumentsSpan() =>
            _fiveArgClosure.CallObjectArguments(_fiveObjectArgs.AsSpan());

        /// <summary>
        /// Calls a Lua closure through the closure caller-owned object storage overload with a slice.
        /// </summary>
        [Benchmark(Description = "Closure Call: object span slice 5 objects")]
        public LuaValue ClosureCallFiveObjectArgumentsSpanSlice() =>
            _fiveArgClosure.CallObjectArguments(_fiveObjectArgsWithPadding.AsSpan(1, 5));

        /// <summary>
        /// Calls a Lua closure through the object-function overload with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: closure object + 3 objects")]
        public LuaValue CallClosureObjectThreeObjects() =>
            _script.Call(_threeArgClosure, _firstObject, _secondObject, _thirdObject);

        /// <summary>
        /// Calls a Lua closure through the object-function overload with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: closure object + 4 objects")]
        public LuaValue CallClosureObjectFourObjects() =>
            _script.Call(_fourArgClosure, _firstObject, _secondObject, _thirdObject, _fourthObject);

        /// <summary>
        /// Calls a Lua closure through the object-function overload with five pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: closure object + 5 objects")]
        public LuaValue CallClosureObjectFiveObjects() =>
            _script.Call(
                _fiveArgClosure,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Calls a Lua closure through the object params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Host Call: params 3 objects")]
        public LuaValue CallThreeObjectsParamsArray() =>
            _script.CallObjectArgumentsCore(
                _threeArgFunction,
                new object[] { _firstObject, _secondObject, _thirdObject }
            );

        /// <summary>
        /// Calls a Lua closure through the object params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Host Call: params 4 objects")]
        public LuaValue CallFourObjectsParamsArray() =>
            _script.CallObjectArgumentsCore(
                _fourArgFunction,
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );

        /// <summary>
        /// Calls a Lua closure through the object params-array overload with caller-owned object storage.
        /// </summary>
        [Benchmark(Description = "Host Call: params 5 objects")]
        public LuaValue CallFiveObjectsParamsArray() =>
            _script.CallObjectArgumentsCore(_fiveArgFunction, _fiveObjectArgs);

        /// <summary>
        /// Calls a Lua closure through the object params-array overload with six objects.
        /// </summary>
        [Benchmark(Description = "Host Call: params 6 objects")]
        public LuaValue CallSixObjectsParamsArray() =>
            _script.CallObjectArgumentsCore(_sixArgFunction, _sixObjectArgs);

        /// <summary>
        /// Calls a Lua closure through the object params-array overload with seven objects.
        /// </summary>
        [Benchmark(Description = "Host Call: params 7 objects")]
        public LuaValue CallSevenObjectsParamsArray() =>
            _script.CallObjectArgumentsCore(_sevenArgFunction, _sevenObjectArgs);

        /// <summary>
        /// Calls a Lua closure with caller-owned contiguous CLR object storage.
        /// </summary>
        [Benchmark(Description = "Host Call: object span 5 objects")]
        public LuaValue CallFiveObjectArgumentsSpan() =>
            _script.CallObjectArguments(_fiveArgFunction, _fiveObjectArgs.AsSpan());

        /// <summary>
        /// Calls a Lua closure with a slice of caller-owned contiguous CLR object storage.
        /// </summary>
        [Benchmark(Description = "Host Call: object span slice 5 objects")]
        public LuaValue CallFiveObjectArgumentsSpanSlice() =>
            _script.CallObjectArguments(_fiveArgFunction, _fiveObjectArgsWithPadding.AsSpan(1, 5));

        /// <summary>
        /// Resumes a suspended Lua coroutine with three pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 3 DynValues")]
        public LuaValue ResumeCoroutineThreeDynValues() =>
            _runningCoroutine.ResumeValues(_first, _second, _third);

        /// <summary>
        /// Resumes a suspended Lua coroutine with four pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 4 DynValues")]
        public LuaValue ResumeCoroutineFourDynValues() =>
            _fourArgRunningCoroutine.ResumeValues(_first, _second, _third, _fourth);

        /// <summary>
        /// Resumes a suspended Lua coroutine with five pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 5 DynValues")]
        public LuaValue ResumeCoroutineFiveDynValues() =>
            _fiveArgRunningCoroutine.ResumeValues(_first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Resumes a suspended Lua coroutine with six pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 6 DynValues")]
        public LuaValue ResumeCoroutineSixDynValues() =>
            _sixArgRunningCoroutine.ResumeValues(_first, _second, _third, _fourth, _fifth, _sixth);

        /// <summary>
        /// Resumes a suspended Lua coroutine with seven pre-created LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 7 DynValues")]
        public LuaValue ResumeCoroutineSevenDynValues() =>
            _sevenArgRunningCoroutine.ResumeValues(
                _first,
                _second,
                _third,
                _fourth,
                _fifth,
                _sixth,
                _seventh
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine with five pre-created DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: span 5 DynValues")]
        public LuaValue ResumeCoroutineFiveDynValuesSpan() =>
            _fiveArgRunningCoroutine.ResumeValues(_fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Resumes a suspended Lua coroutine with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 3 objects")]
        public LuaValue ResumeCoroutineThreeObjects() =>
            _runningCoroutine.Resume(_firstObject, _secondObject, _thirdObject);

        /// <summary>
        /// Resumes a suspended Lua coroutine with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 4 objects")]
        public LuaValue ResumeCoroutineFourObjects() =>
            _fourArgRunningCoroutine.Resume(
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine with five pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 5 objects")]
        public LuaValue ResumeCoroutineFiveObjects() =>
            _fiveArgRunningCoroutine.Resume(
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine with six pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 6 objects")]
        public LuaValue ResumeCoroutineSixObjects() =>
            _sixArgRunningCoroutine.Resume(
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                _sixthObject
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine with seven pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 7 objects")]
        public LuaValue ResumeCoroutineSevenObjects() =>
            _sevenArgRunningCoroutine.Resume(
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                _sixthObject,
                _seventhObject
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine with five CLR object arguments from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: object span 5 objects")]
        public LuaValue ResumeCoroutineFiveObjectArgumentsSpan() =>
            _fiveArgRunningCoroutine.ResumeObjectArguments(_fiveObjectArgs.AsSpan());

        /// <summary>
        /// Resumes a suspended Lua coroutine with five CLR object arguments from a caller-owned slice.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: object span slice 5 objects")]
        public LuaValue ResumeCoroutineFiveObjectArgumentsSpanSlice() =>
            _fiveArgRunningCoroutine.ResumeObjectArguments(_fiveObjectArgsWithPadding.AsSpan(1, 5));

        /// <summary>
        /// Resumes a suspended Lua coroutine through the params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 3 DynValues")]
        public LuaValue ResumeCoroutineThreeDynValuesParamsArray() =>
            _runningCoroutine.ResumeValues(new LuaValue[] { _first, _second, _third });

        /// <summary>
        /// Resumes a suspended Lua coroutine through the params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 4 DynValues")]
        public LuaValue ResumeCoroutineFourDynValuesParamsArray() =>
            _fourArgRunningCoroutine.ResumeValues(
                new LuaValue[] { _first, _second, _third, _fourth }
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine through the object params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 3 objects")]
        public LuaValue ResumeCoroutineThreeObjectsParamsArray() =>
            _runningCoroutine.ResumeObjectArguments(
                new object[] { _firstObject, _secondObject, _thirdObject }
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine through the object params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 4 objects")]
        public LuaValue ResumeCoroutineFourObjectsParamsArray() =>
            _fourArgRunningCoroutine.ResumeObjectArguments(
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine through the object params-array overload with five objects.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 5 objects")]
        public LuaValue ResumeCoroutineFiveObjectsParamsArray() =>
            _fiveArgRunningCoroutine.ResumeObjectArguments(
                new object[]
                {
                    _firstObject,
                    _secondObject,
                    _thirdObject,
                    _fourthObject,
                    _fifthObject,
                }
            );
    }

    /// <summary>
    /// Benchmarks CLR object to <see cref="LuaValue"/> conversion for common host API inputs.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class ObjectConversionBenchmarks
    {
        private Script _script;
        private object _intObject = 42;
        private object _doubleObject = 3.5d;
        private object _boolObject = true;
        private object _stringObject = "payload";
        private object _closureObject;
        private object _callbackObject;

        /// <summary>
        /// Prepares stable boxed inputs for conversion.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            LuaValue closure = _script.DoString("return function(value) return value end");
            _closureObject = closure.Function;
            _callbackObject = new CallbackFunction((_, _) => LuaValue.Nil);
        }

        /// <summary>
        /// Converts a boxed integer to a Lua integer value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: int")]
        public LuaValue FromInt() => LuaValue.FromObject(_script, _intObject);

        /// <summary>
        /// Converts a boxed double to a Lua number value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: double")]
        public LuaValue FromDouble() => LuaValue.FromObject(_script, _doubleObject);

        /// <summary>
        /// Converts a boxed Boolean to a cached Lua Boolean value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: bool")]
        public LuaValue FromBool() => LuaValue.FromObject(_script, _boolObject);

        /// <summary>
        /// Converts a CLR string to a Lua string value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: string")]
        public LuaValue FromString() => LuaValue.FromObject(_script, _stringObject);

        /// <summary>
        /// Converts a closure object through the cached wrapper path.
        /// </summary>
        [Benchmark(Description = "Object Conversion: closure")]
        public LuaValue FromClosure() => LuaValue.FromObject(_script, _closureObject);

        /// <summary>
        /// Converts a callback function object through the cached wrapper path.
        /// </summary>
        [Benchmark(Description = "Object Conversion: callback")]
        public LuaValue FromCallback() => LuaValue.FromObject(_script, _callbackObject);
    }

    /// <summary>
    /// Benchmarks host calls into CLR callbacks through legacy and argument-view APIs.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class ClrCallbackCallBenchmarks
    {
        private Script _script;
        private LuaValue _legacyCallback = LuaValue.Nil;
        private LuaValue _viewCallback = LuaValue.Nil;
        private LuaValue _viewNoContextCallback = LuaValue.Nil;
        private LuaValue _first = LuaValue.Nil;
        private LuaValue _second = LuaValue.Nil;
        private LuaValue _third = LuaValue.Nil;
        private LuaValue _fourth = LuaValue.Nil;
        private LuaValue _fifth = LuaValue.Nil;
        private LuaValue[] _threeDynValueArgs = Array.Empty<LuaValue>();
        private LuaValue[] _fourDynValueArgs = Array.Empty<LuaValue>();
        private LuaValue[] _fiveDynValueArgs = Array.Empty<LuaValue>();

        /// <summary>
        /// Prepares stable callback and argument values for CLR callback call benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _legacyCallback = LuaValue.NewCallback((_, args) => args[args.Count - 1]);
            _viewCallback = LuaValue.NewCallbackView((_, args) => args[args.Count - 1]);
            _viewNoContextCallback = LuaValue.NewCallbackView(
                (CallbackArgumentsView args) => args[args.Count - 1]
            );
            _first = LuaValue.NewNumber(1d);
            _second = LuaValue.NewNumber(2d);
            _third = LuaValue.NewNumber(3d);
            _fourth = LuaValue.NewNumber(4d);
            _fifth = LuaValue.NewNumber(5d);
            _threeDynValueArgs = new[] { _first, _second, _third };
            _fourDynValueArgs = new[] { _first, _second, _third, _fourth };
            _fiveDynValueArgs = new[] { _first, _second, _third, _fourth, _fifth };
        }

        /// <summary>
        /// Calls a legacy CLR callback with three fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: 3 fixed DynValues")]
        public LuaValue CallLegacyThreeDynValues() =>
            _script.CallValues(_legacyCallback, _first, _second, _third);

        /// <summary>
        /// Calls an argument-view CLR callback with three fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: 3 fixed DynValues")]
        public LuaValue CallViewThreeDynValues() =>
            _script.CallValues(_viewCallback, _first, _second, _third);

        /// <summary>
        /// Calls a contextless argument-view CLR callback with three fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: 3 fixed DynValues")]
        public LuaValue CallViewNoContextThreeDynValues() =>
            _script.CallValues(_viewNoContextCallback, _first, _second, _third);

        /// <summary>
        /// Calls a legacy CLR callback through the params-array overload with three LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: params 3 DynValues")]
        public LuaValue CallLegacyThreeDynValuesParamsArray() =>
            _script.CallValues(_legacyCallback, new LuaValue[] { _first, _second, _third });

        /// <summary>
        /// Calls an argument-view CLR callback through the params-array overload with three LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: params 3 DynValues")]
        public LuaValue CallViewThreeDynValuesParamsArray() =>
            _script.CallValues(_viewCallback, new LuaValue[] { _first, _second, _third });

        /// <summary>
        /// Calls a contextless argument-view CLR callback through the params-array overload with three LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: params 3 DynValues")]
        public LuaValue CallViewNoContextThreeDynValuesParamsArray() =>
            _script.CallValues(_viewNoContextCallback, new LuaValue[] { _first, _second, _third });

        /// <summary>
        /// Calls a legacy CLR callback with three DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: span 3 DynValues")]
        public LuaValue CallLegacyThreeDynValuesSpan() =>
            _script.CallValues(_legacyCallback, _threeDynValueArgs.AsSpan());

        /// <summary>
        /// Calls an argument-view CLR callback with three DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: span 3 DynValues")]
        public LuaValue CallViewThreeDynValuesSpan() =>
            _script.CallValues(_viewCallback, _threeDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a contextless argument-view CLR callback with three DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: span 3 DynValues")]
        public LuaValue CallViewNoContextThreeDynValuesSpan() =>
            _script.CallValues(_viewNoContextCallback, _threeDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a legacy CLR callback with four fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: 4 fixed DynValues")]
        public LuaValue CallLegacyFourDynValues() =>
            _script.CallValues(_legacyCallback, _first, _second, _third, _fourth);

        /// <summary>
        /// Calls an argument-view CLR callback with four fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: 4 fixed DynValues")]
        public LuaValue CallViewFourDynValues() =>
            _script.CallValues(_viewCallback, _first, _second, _third, _fourth);

        /// <summary>
        /// Calls a contextless argument-view CLR callback with four fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: 4 fixed DynValues")]
        public LuaValue CallViewNoContextFourDynValues() =>
            _script.CallValues(_viewNoContextCallback, _first, _second, _third, _fourth);

        /// <summary>
        /// Calls a legacy CLR callback through the params-array overload with four LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: params 4 DynValues")]
        public LuaValue CallLegacyFourDynValuesParamsArray() =>
            _script.CallValues(
                _legacyCallback,
                new LuaValue[] { _first, _second, _third, _fourth }
            );

        /// <summary>
        /// Calls an argument-view CLR callback through the params-array overload with four LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: params 4 DynValues")]
        public LuaValue CallViewFourDynValuesParamsArray() =>
            _script.CallValues(_viewCallback, new LuaValue[] { _first, _second, _third, _fourth });

        /// <summary>
        /// Calls a contextless argument-view CLR callback through the params-array overload with four LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: params 4 DynValues")]
        public LuaValue CallViewNoContextFourDynValuesParamsArray() =>
            _script.CallValues(
                _viewNoContextCallback,
                new LuaValue[] { _first, _second, _third, _fourth }
            );

        /// <summary>
        /// Calls a legacy CLR callback with four DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: span 4 DynValues")]
        public LuaValue CallLegacyFourDynValuesSpan() =>
            _script.CallValues(_legacyCallback, _fourDynValueArgs.AsSpan());

        /// <summary>
        /// Calls an argument-view CLR callback with four DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: span 4 DynValues")]
        public LuaValue CallViewFourDynValuesSpan() =>
            _script.CallValues(_viewCallback, _fourDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a contextless argument-view CLR callback with four DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: span 4 DynValues")]
        public LuaValue CallViewNoContextFourDynValuesSpan() =>
            _script.CallValues(_viewNoContextCallback, _fourDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a legacy CLR callback with five fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: 5 fixed DynValues")]
        public LuaValue CallLegacyFiveDynValues() =>
            _script.CallValues(_legacyCallback, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls an argument-view CLR callback with five fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: 5 fixed DynValues")]
        public LuaValue CallViewFiveDynValues() =>
            _script.CallValues(_viewCallback, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a contextless argument-view CLR callback with five fixed LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: 5 fixed DynValues")]
        public LuaValue CallViewNoContextFiveDynValues() =>
            _script.CallValues(_viewNoContextCallback, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a legacy CLR callback through the params-array overload with five LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: params 5 DynValues")]
        public LuaValue CallLegacyFiveDynValuesParamsArray() =>
            _script.CallValues(
                _legacyCallback,
                new LuaValue[] { _first, _second, _third, _fourth, _fifth }
            );

        /// <summary>
        /// Calls an argument-view CLR callback through the params-array overload with five LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: params 5 DynValues")]
        public LuaValue CallViewFiveDynValuesParamsArray() =>
            _script.CallValues(
                _viewCallback,
                new LuaValue[] { _first, _second, _third, _fourth, _fifth }
            );

        /// <summary>
        /// Calls a contextless argument-view CLR callback through the params-array overload with five LuaValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: params 5 DynValues")]
        public LuaValue CallViewNoContextFiveDynValuesParamsArray() =>
            _script.CallValues(
                _viewNoContextCallback,
                new LuaValue[] { _first, _second, _third, _fourth, _fifth }
            );

        /// <summary>
        /// Calls a legacy CLR callback with five DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: span 5 DynValues")]
        public LuaValue CallLegacyFiveDynValuesSpan() =>
            _script.CallValues(_legacyCallback, _fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Calls an argument-view CLR callback with five DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: span 5 DynValues")]
        public LuaValue CallViewFiveDynValuesSpan() =>
            _script.CallValues(_viewCallback, _fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a contextless argument-view CLR callback with five DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View NoContext Call: span 5 DynValues")]
        public LuaValue CallViewNoContextFiveDynValuesSpan() =>
            _script.CallValues(_viewNoContextCallback, _fiveDynValueArgs.AsSpan());
    }

    /// <summary>
    /// Benchmarks Lua continuation paths used by protected calls and metamethod post-processing.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class ContinuationBenchmarks
    {
        private Script _script;
        private LuaValue _pcallNoReturnFunction = LuaValue.Nil;
        private LuaValue _pcallOneReturnFunction = LuaValue.Nil;
        private LuaValue _tostringMetamethodFunction = LuaValue.Nil;

        /// <summary>
        /// Prepares small Lua functions that exercise continuation callbacks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _pcallNoReturnFunction = _script.DoString(
                """
                local function inner()
                end

                return function()
                    local ok = pcall(inner)
                    return ok
                end
                """
            );
            _pcallOneReturnFunction = _script.DoString(
                """
                local function inner()
                    return 42
                end

                return function()
                    local ok, value = pcall(inner)
                    if ok then
                        return value
                    end
                    return 0
                end
                """
            );
            _tostringMetamethodFunction = _script.DoString(
                """
                local target = setmetatable({}, {
                    __tostring = function()
                        return 'value'
                    end
                })

                return function()
                    return tostring(target)
                end
                """
            );
        }

        /// <summary>
        /// Executes a successful protected call whose callee returns no values.
        /// </summary>
        [Benchmark(Description = "Continuation: pcall no return")]
        public LuaValue PcallNoReturn() => _script.CallValues(_pcallNoReturnFunction);

        /// <summary>
        /// Executes a successful protected call whose callee returns one value.
        /// </summary>
        [Benchmark(Description = "Continuation: pcall one return")]
        public LuaValue PcallOneReturn() => _script.CallValues(_pcallOneReturnFunction);

        /// <summary>
        /// Executes <c>tostring</c> through a table <c>__tostring</c> metamethod.
        /// </summary>
        [Benchmark(Description = "Continuation: tostring metamethod")]
        public LuaValue TostringMetamethod() => _script.CallValues(_tostringMetamethodFunction);
    }

    /// <summary>
    /// Benchmarks Lua bytecode calling CLR callbacks.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class LuaToClrCallbackCallBenchmarks
    {
        private Script _script;
        private LuaValue _legacyThree = LuaValue.Nil;
        private LuaValue _viewThree = LuaValue.Nil;
        private LuaValue _viewNoContextThree = LuaValue.Nil;
        private LuaValue _legacyFour = LuaValue.Nil;
        private LuaValue _viewFour = LuaValue.Nil;
        private LuaValue _viewNoContextFour = LuaValue.Nil;
        private LuaValue _legacyFive = LuaValue.Nil;
        private LuaValue _viewFive = LuaValue.Nil;
        private LuaValue _viewNoContextFive = LuaValue.Nil;
        private LuaValue _legacySpanProbeFour = LuaValue.Nil;

        /// <summary>
        /// Prepares Lua closures that call CLR callbacks from bytecode.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _script.Globals["legacy"] = LuaValue.NewCallback((_, args) => args[args.Count - 1]);
            _script.Globals["view"] = LuaValue.NewCallbackView((_, args) => args[args.Count - 1]);
            _script.Globals["viewNoContext"] = LuaValue.NewCallbackView(
                (CallbackArgumentsView args) => args[args.Count - 1]
            );
            _script.Globals["legacySpanProbe"] = LuaValue.NewCallback(
                (_, args) =>
                    args.TryGetSpan(out ReadOnlySpan<LuaValue> span)
                        ? span[span.Length - 1]
                        : args[args.Count - 1]
            );
            _legacyThree = _script.DoString("return function() return legacy(1, 2, 3) end");
            _viewThree = _script.DoString("return function() return view(1, 2, 3) end");
            _viewNoContextThree = _script.DoString(
                "return function() return viewNoContext(1, 2, 3) end"
            );
            _legacyFour = _script.DoString("return function() return legacy(1, 2, 3, 4) end");
            _viewFour = _script.DoString("return function() return view(1, 2, 3, 4) end");
            _viewNoContextFour = _script.DoString(
                "return function() return viewNoContext(1, 2, 3, 4) end"
            );
            _legacyFive = _script.DoString("return function() return legacy(1, 2, 3, 4, 5) end");
            _viewFive = _script.DoString("return function() return view(1, 2, 3, 4, 5) end");
            _viewNoContextFive = _script.DoString(
                "return function() return viewNoContext(1, 2, 3, 4, 5) end"
            );
            _legacySpanProbeFour = _script.DoString(
                "return function() return legacySpanProbe(1, 2, 3, 4) end"
            );
        }

        /// <summary>
        /// Runs Lua bytecode that calls a legacy CLR callback with three arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback Legacy: 3 args")]
        public LuaValue CallLegacyThreeArgs() => _script.CallValues(_legacyThree);

        /// <summary>
        /// Runs Lua bytecode that calls an argument-view CLR callback with three arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View: 3 args")]
        public LuaValue CallViewThreeArgs() => _script.CallValues(_viewThree);

        /// <summary>
        /// Runs Lua bytecode that calls a contextless argument-view CLR callback with three arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View NoContext: 3 args")]
        public LuaValue CallViewNoContextThreeArgs() => _script.CallValues(_viewNoContextThree);

        /// <summary>
        /// Runs Lua bytecode that calls a legacy CLR callback with four arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback Legacy: 4 args")]
        public LuaValue CallLegacyFourArgs() => _script.CallValues(_legacyFour);

        /// <summary>
        /// Runs Lua bytecode that calls an argument-view CLR callback with four arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View: 4 args")]
        public LuaValue CallViewFourArgs() => _script.CallValues(_viewFour);

        /// <summary>
        /// Runs Lua bytecode that calls a contextless argument-view CLR callback with four arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View NoContext: 4 args")]
        public LuaValue CallViewNoContextFourArgs() => _script.CallValues(_viewNoContextFour);

        /// <summary>
        /// Runs Lua bytecode that calls a legacy CLR callback with five arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback Legacy: 5 args")]
        public LuaValue CallLegacyFiveArgs() => _script.CallValues(_legacyFive);

        /// <summary>
        /// Runs Lua bytecode that calls an argument-view CLR callback with five arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View: 5 args")]
        public LuaValue CallViewFiveArgs() => _script.CallValues(_viewFive);

        /// <summary>
        /// Runs Lua bytecode that calls a contextless argument-view CLR callback with five arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View NoContext: 5 args")]
        public LuaValue CallViewNoContextFiveArgs() => _script.CallValues(_viewNoContextFive);

        /// <summary>
        /// Runs Lua bytecode that calls a legacy CLR callback and consumes the VM-backed argument span.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback Legacy TryGetSpan: 4 args")]
        public LuaValue CallLegacySpanProbeFourArgs() => _script.CallValues(_legacySpanProbeFour);
    }

    /// <summary>
    /// Benchmarks CLR callbacks calling back into Lua through <see cref="ScriptExecutionContext"/>.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class ScriptExecutionContextCallBenchmarks
    {
        private Script _script;
        private LuaValue _threeArgFunction = LuaValue.Nil;
        private LuaValue _fourArgFunction = LuaValue.Nil;
        private LuaValue _fiveArgFunction = LuaValue.Nil;
        private LuaValue _sixArgFunction = LuaValue.Nil;
        private LuaValue _sevenArgFunction = LuaValue.Nil;
        private LuaValue _contextFixedThreeCallback = LuaValue.Nil;
        private LuaValue _contextParamsThreeCallback = LuaValue.Nil;
        private LuaValue _contextFixedFourCallback = LuaValue.Nil;
        private LuaValue _contextParamsFourCallback = LuaValue.Nil;
        private LuaValue _contextFixedFiveCallback = LuaValue.Nil;
        private LuaValue _contextParamsFiveCallback = LuaValue.Nil;
        private LuaValue _contextSpanFiveCallback = LuaValue.Nil;
        private LuaValue _contextFixedSixCallback = LuaValue.Nil;
        private LuaValue _contextFixedSevenCallback = LuaValue.Nil;
        private LuaValue _contextSpanSevenCallback = LuaValue.Nil;
        private LuaValue _first = LuaValue.Nil;
        private LuaValue _second = LuaValue.Nil;
        private LuaValue _third = LuaValue.Nil;
        private LuaValue _fourth = LuaValue.Nil;
        private LuaValue _fifth = LuaValue.Nil;
        private LuaValue _sixth = LuaValue.Nil;
        private LuaValue _seventh = LuaValue.Nil;
        private LuaValue[] _fiveDynValueArgs = Array.Empty<LuaValue>();
        private LuaValue[] _sevenDynValueArgs = Array.Empty<LuaValue>();

        /// <summary>
        /// Prepares callback-to-Lua call benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _threeArgFunction = _script.DoString("return function(a, b, c) return c end");
            _fourArgFunction = _script.DoString("return function(a, b, c, d) return d end");
            _fiveArgFunction = _script.DoString("return function(a, b, c, d, e) return e end");
            _sixArgFunction = _script.DoString("return function(a, b, c, d, e, f) return f end");
            _sevenArgFunction = _script.DoString(
                "return function(a, b, c, d, e, f, g) return g end"
            );
            _first = LuaValue.NewNumber(1d);
            _second = LuaValue.NewNumber(2d);
            _third = LuaValue.NewNumber(3d);
            _fourth = LuaValue.NewNumber(4d);
            _fifth = LuaValue.NewNumber(5d);
            _sixth = LuaValue.NewNumber(6d);
            _seventh = LuaValue.NewNumber(7d);
            _fiveDynValueArgs = new[] { _first, _second, _third, _fourth, _fifth };
            _sevenDynValueArgs = new[]
            {
                _first,
                _second,
                _third,
                _fourth,
                _fifth,
                _sixth,
                _seventh,
            };
            _contextFixedThreeCallback = LuaValue.NewCallbackView(
                (context, _) => context.Call(_threeArgFunction, _first, _second, _third)
            );
            _contextParamsThreeCallback = LuaValue.NewCallbackView(
                (context, _) =>
                    context.Call(_threeArgFunction, new LuaValue[] { _first, _second, _third })
            );
            _contextFixedFourCallback = LuaValue.NewCallbackView(
                (context, _) => context.Call(_fourArgFunction, _first, _second, _third, _fourth)
            );
            _contextParamsFourCallback = LuaValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        _fourArgFunction,
                        new LuaValue[] { _first, _second, _third, _fourth }
                    )
            );
            _contextFixedFiveCallback = LuaValue.NewCallbackView(
                (context, _) =>
                    context.Call(_fiveArgFunction, _first, _second, _third, _fourth, _fifth)
            );
            _contextParamsFiveCallback = LuaValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        _fiveArgFunction,
                        new LuaValue[] { _first, _second, _third, _fourth, _fifth }
                    )
            );
            _contextSpanFiveCallback = LuaValue.NewCallbackView(
                (context, _) => context.Call(_fiveArgFunction, _fiveDynValueArgs.AsSpan())
            );
            _contextFixedSixCallback = LuaValue.NewCallbackView(
                (context, _) =>
                    context.Call(_sixArgFunction, _first, _second, _third, _fourth, _fifth, _sixth)
            );
            _contextFixedSevenCallback = LuaValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        _sevenArgFunction,
                        _first,
                        _second,
                        _third,
                        _fourth,
                        _fifth,
                        _sixth,
                        _seventh
                    )
            );
            _contextSpanSevenCallback = LuaValue.NewCallbackView(
                (context, _) => context.Call(_sevenArgFunction, _sevenDynValueArgs.AsSpan())
            );
        }

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed three-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 3 fixed DynValues")]
        public LuaValue CallContextThreeDynValues() =>
            _script.CallValues(_contextFixedThreeCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the params-array context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: params 3 DynValues")]
        public LuaValue CallContextThreeDynValuesParamsArray() =>
            _script.CallValues(_contextParamsThreeCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed four-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 4 fixed DynValues")]
        public LuaValue CallContextFourDynValues() => _script.CallValues(_contextFixedFourCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the params-array context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: params 4 DynValues")]
        public LuaValue CallContextFourDynValuesParamsArray() =>
            _script.CallValues(_contextParamsFourCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed five-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 5 fixed DynValues")]
        public LuaValue CallContextFiveDynValues() => _script.CallValues(_contextFixedFiveCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the params-array context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: params 5 DynValues")]
        public LuaValue CallContextFiveDynValuesParamsArray() =>
            _script.CallValues(_contextParamsFiveCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the span context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: span 5 DynValues")]
        public LuaValue CallContextFiveDynValuesSpan() =>
            _script.CallValues(_contextSpanFiveCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed six-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 6 fixed DynValues")]
        public LuaValue CallContextSixDynValues() => _script.CallValues(_contextFixedSixCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed seven-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 7 fixed DynValues")]
        public LuaValue CallContextSevenDynValues() =>
            _script.CallValues(_contextFixedSevenCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the span context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: span 7 DynValues")]
        public LuaValue CallContextSevenDynValuesSpan() =>
            _script.CallValues(_contextSpanSevenCallback);
    }

    /// <summary>
    /// Benchmarks host-side nested table access through fixed key overloads and params-array paths.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class TableAccessBenchmarks
    {
        private Script _script;
        private Table _table;
        private object[] _twoKeys = Array.Empty<object>();
        private object[] _threeKeys = Array.Empty<object>();
        private object[] _paddedTwoKeys = Array.Empty<object>();
        private object[] _paddedThreeKeys = Array.Empty<object>();
        private LuaValue _value = LuaValue.Nil;

        /// <summary>
        /// Builds a stable nested table graph for host-side lookup and mutation benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _table = new Table(_script);
            Table child = new(_script);
            Table grandchild = new(_script);
            _value = LuaValue.NewNumber(42);

            _table.Set("child", LuaValue.NewTable(child));
            child.Set("grandchild", LuaValue.NewTable(grandchild));
            child.Set("leaf", _value);
            grandchild.Set("leaf", _value);
            _twoKeys = new object[] { "child", "leaf" };
            _threeKeys = new object[] { "child", "grandchild", "leaf" };
            _paddedTwoKeys = new object[] { "ignored", "child", "leaf", "ignored" };
            _paddedThreeKeys = new object[] { "ignored", "child", "grandchild", "leaf", "ignored" };
        }

        /// <summary>
        /// Reads a nested value through the fixed two-key raw lookup overload.
        /// </summary>
        [Benchmark(Description = "Table RawGet: 2 fixed keys")]
        public LuaValue RawGetTwoFixedKeys() => _table.RawGet("child", "leaf");

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table RawGet: array 2 keys")]
        public LuaValue RawGetTwoArrayKeys() => _table.RawGet(_twoKeys);

        /// <summary>
        /// Reads a nested value through the span-backed raw lookup overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table RawGet: span 2 keys")]
        public LuaValue RawGetTwoSpanKeys() => _table.RawGet(_twoKeys.AsSpan());

        /// <summary>
        /// Reads a nested value through the span-backed raw lookup overload with a caller-owned slice.
        /// </summary>
        [Benchmark(Description = "Table RawGet: span slice 2 keys")]
        public LuaValue RawGetTwoSpanSliceKeys() => _table.RawGet(_paddedTwoKeys.AsSpan(1, 2));

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Table RawGet: new array 2 keys")]
        public LuaValue RawGetTwoNewArrayKeys() => _table.RawGet(new object[] { "child", "leaf" });

        /// <summary>
        /// Reads a nested value through the fixed three-key raw lookup overload.
        /// </summary>
        [Benchmark(Description = "Table RawGet: 3 fixed keys")]
        public LuaValue RawGetThreeFixedKeys() => _table.RawGet("child", "grandchild", "leaf");

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table RawGet: array 3 keys")]
        public LuaValue RawGetThreeArrayKeys() => _table.RawGet(_threeKeys);

        /// <summary>
        /// Reads a nested value through the span-backed raw lookup overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table RawGet: span 3 keys")]
        public LuaValue RawGetThreeSpanKeys() => _table.RawGet(_threeKeys.AsSpan());

        /// <summary>
        /// Reads a nested value through the span-backed raw lookup overload with a caller-owned slice.
        /// </summary>
        [Benchmark(Description = "Table RawGet: span slice 3 keys")]
        public LuaValue RawGetThreeSpanSliceKeys() => _table.RawGet(_paddedThreeKeys.AsSpan(1, 3));

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Table RawGet: new array 3 keys")]
        public LuaValue RawGetThreeNewArrayKeys() =>
            _table.RawGet(new object[] { "child", "grandchild", "leaf" });

        /// <summary>
        /// Reads a nested value through the fixed two-key lookup overload.
        /// </summary>
        [Benchmark(Description = "Table Get: 2 fixed keys")]
        public LuaValue GetTwoFixedKeys() => _table.Get("child", "leaf");

        /// <summary>
        /// Reads a nested value through the span-backed lookup overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table Get: span 2 keys")]
        public LuaValue GetTwoSpanKeys() => _table.Get(_twoKeys.AsSpan());

        /// <summary>
        /// Reads a nested value through the span-backed lookup overload with a caller-owned slice.
        /// </summary>
        [Benchmark(Description = "Table Get: span slice 2 keys")]
        public LuaValue GetTwoSpanSliceKeys() => _table.Get(_paddedTwoKeys.AsSpan(1, 2));

        /// <summary>
        /// Reads a nested value through the array-backed lookup overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Table Get: new array 2 keys")]
        public LuaValue GetTwoNewArrayKeys() => _table.Get(new object[] { "child", "leaf" });

        /// <summary>
        /// Reads a nested value through the fixed two-key indexer overload.
        /// </summary>
        [Benchmark(Description = "Table Indexer: 2 fixed keys")]
        public object IndexerTwoFixedKeys() => _table["child", "leaf"];

        /// <summary>
        /// Writes a nested value through the fixed two-key setter overload.
        /// </summary>
        [Benchmark(Description = "Table Set: 2 fixed keys")]
        public void SetTwoFixedKeys() => _table.Set("child", "leaf", _value);

        /// <summary>
        /// Writes a nested value through the array-backed setter overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table Set: array 2 keys")]
        public void SetTwoArrayKeys() => _table.Set(_twoKeys, _value);

        /// <summary>
        /// Writes a nested value through the span-backed setter overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table Set: span 2 keys")]
        public void SetTwoSpanKeys() => _table.Set(_twoKeys.AsSpan(), _value);

        /// <summary>
        /// Writes a nested value through the span-backed setter overload with a caller-owned slice.
        /// </summary>
        [Benchmark(Description = "Table Set: span slice 2 keys")]
        public void SetTwoSpanSliceKeys() => _table.Set(_paddedTwoKeys.AsSpan(1, 2), _value);

        /// <summary>
        /// Writes a nested value through the array-backed setter overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Table Set: new array 2 keys")]
        public void SetTwoNewArrayKeys() => _table.Set(new object[] { "child", "leaf" }, _value);
    }

    /// <summary>
    /// Benchmarks Unity-facing mod function calls through fixed overloads and params-array paths.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class ModCallBenchmarks
    {
        private ModContainer _mod;
        private ModManager _manager;
        private object _firstObject = 1d;
        private object _secondObject = 2d;
        private object _thirdObject = 3d;
        private object _fourthObject = 4d;
        private object _fifthObject = 5d;
        private object[] _modTwoObjectArgs = Array.Empty<object>();
        private object[] _modFiveObjectArgs = Array.Empty<object>();
        private object[] _modPaddedFiveObjectArgs = Array.Empty<object>();

        /// <summary>
        /// Loads a mod and manager with small Lua functions used by the call benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _firstObject = 1d;
            _secondObject = 2d;
            _thirdObject = 3d;
            _fourthObject = 4d;
            _fifthObject = 5d;
            _modTwoObjectArgs = new object[] { _firstObject, _secondObject };
            _modFiveObjectArgs = new object[]
            {
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
            };
            _modPaddedFiveObjectArgs = new object[]
            {
                -1d,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject,
                -1d,
            };

            _mod = new ModContainer("bench").AddEntryPoint(
                """
                function second(a, b) return b end
                function fourth(a, b, c, d) return d end
                function fifth(a, b, c, d, e) return e end
                """
            );
            _mod.Load();

            _manager = new ModManager();
            _manager.Register(
                new ModContainer("bench").AddEntryPoint(
                    """
                    function second(a, b) return b end
                    function fourth(a, b, c, d) return d end
                    function fifth(a, b, c, d, e) return e end
                    """
                )
            );
            _manager.LoadAll();
        }

        /// <summary>
        /// Calls a mod function through the fixed two-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: 2 fixed objects")]
        public LuaValue CallFunctionTwoFixedObjects() =>
            _mod.CallFunction("second", _firstObject, _secondObject);

        /// <summary>
        /// Calls a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: params 2 objects")]
        public LuaValue CallFunctionTwoParamsArray() =>
            _mod.CallFunction("second", new object[] { _firstObject, _secondObject });

        /// <summary>
        /// Calls a mod function through the caller-owned span two-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: span 2 objects")]
        public LuaValue CallFunctionTwoObjectSpan() =>
            _mod.CallFunctionObjectArguments("second", _modTwoObjectArgs.AsSpan());

        /// <summary>
        /// Calls a mod function through the fixed four-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: 4 fixed objects")]
        public LuaValue CallFunctionFourFixedObjects() =>
            _mod.CallFunction("fourth", _firstObject, _secondObject, _thirdObject, _fourthObject);

        /// <summary>
        /// Calls a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: params 4 objects")]
        public LuaValue CallFunctionFourParamsArray() =>
            _mod.CallFunction(
                "fourth",
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );

        /// <summary>
        /// Calls a mod function through the fixed five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: 5 fixed objects")]
        public LuaValue CallFunctionFiveFixedObjects() =>
            _mod.CallFunction(
                "fifth",
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Calls a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: params 5 objects")]
        public LuaValue CallFunctionFiveParamsArray() =>
            _mod.CallFunction(
                "fifth",
                new object[]
                {
                    _firstObject,
                    _secondObject,
                    _thirdObject,
                    _fourthObject,
                    _fifthObject,
                }
            );

        /// <summary>
        /// Calls a mod function through the caller-owned span five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: span 5 objects")]
        public LuaValue CallFunctionFiveObjectSpan() =>
            _mod.CallFunctionObjectArguments("fifth", _modFiveObjectArgs.AsSpan());

        /// <summary>
        /// Calls a mod function through the caller-owned span slice five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: span slice 5 objects")]
        public LuaValue CallFunctionFiveObjectSpanSlice() =>
            _mod.CallFunctionObjectArguments("fifth", _modPaddedFiveObjectArgs.AsSpan(1, 5));

        /// <summary>
        /// Broadcasts a mod function through the fixed two-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: 2 fixed objects")]
        public IDictionary<string, LuaValue> BroadcastCallTwoFixedObjects() =>
            _manager.BroadcastCall("second", _firstObject, _secondObject);

        /// <summary>
        /// Broadcasts a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: params 2 objects")]
        public IDictionary<string, LuaValue> BroadcastCallTwoParamsArray() =>
            _manager.BroadcastCall("second", new object[] { _firstObject, _secondObject });

        /// <summary>
        /// Broadcasts a mod function through the caller-owned span two-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: span 2 objects")]
        public IDictionary<string, LuaValue> BroadcastCallTwoObjectSpan() =>
            _manager.BroadcastCallObjectArguments("second", _modTwoObjectArgs.AsSpan());

        /// <summary>
        /// Broadcasts a mod function through the fixed four-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: 4 fixed objects")]
        public IDictionary<string, LuaValue> BroadcastCallFourFixedObjects() =>
            _manager.BroadcastCall(
                "fourth",
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject
            );

        /// <summary>
        /// Broadcasts a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: params 4 objects")]
        public IDictionary<string, LuaValue> BroadcastCallFourParamsArray() =>
            _manager.BroadcastCall(
                "fourth",
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );

        /// <summary>
        /// Broadcasts a mod function through the fixed five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: 5 fixed objects")]
        public IDictionary<string, LuaValue> BroadcastCallFiveFixedObjects() =>
            _manager.BroadcastCall(
                "fifth",
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Broadcasts a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: params 5 objects")]
        public IDictionary<string, LuaValue> BroadcastCallFiveParamsArray() =>
            _manager.BroadcastCall(
                "fifth",
                new object[]
                {
                    _firstObject,
                    _secondObject,
                    _thirdObject,
                    _fourthObject,
                    _fifthObject,
                }
            );

        /// <summary>
        /// Broadcasts a mod function through the caller-owned span five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: span 5 objects")]
        public IDictionary<string, LuaValue> BroadcastCallFiveObjectSpan() =>
            _manager.BroadcastCallObjectArguments("fifth", _modFiveObjectArgs.AsSpan());

        /// <summary>
        /// Broadcasts a mod function through the caller-owned span slice five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: span slice 5 objects")]
        public IDictionary<string, LuaValue> BroadcastCallFiveObjectSpanSlice() =>
            _manager.BroadcastCallObjectArguments("fifth", _modPaddedFiveObjectArgs.AsSpan(1, 5));
    }
}
