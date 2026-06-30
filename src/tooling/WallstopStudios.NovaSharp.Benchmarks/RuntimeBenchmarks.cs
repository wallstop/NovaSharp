namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
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
        private DynValue _compiledEntry = DynValue.Nil;
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
                RuntimeScenario.NumericLoops => () => _script.Call(_compiledEntry).Number,
                RuntimeScenario.TableMutation => RunTableScenario,
                RuntimeScenario.CoroutinePipeline => RunCoroutineScenario,
                RuntimeScenario.UserDataInterop => RunUserDataScenario,
                _ => () => _script.Call(_compiledEntry).Number,
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
                table.Set(i, DynValue.NewNumber(i * 1.5));
            }

            return _script.Call(_compiledEntry, table).Number;
        }

        private double RunCoroutineScenario() =>
            _script.Call(_compiledEntry, LuaRuntimeSuites.CoroutineSteps).Number;

        private double RunUserDataScenario()
        {
            _host.Reset();
            return _script.Call(_compiledEntry, _host, LuaRuntimeSuites.UserDataIterations).Number;
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
    /// Benchmarks host-to-Lua calls through fixed DynValue public API overloads.
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
        private DynValue _oneArgFunction = DynValue.Nil;
        private DynValue _twoArgFunction = DynValue.Nil;
        private DynValue _threeArgFunction = DynValue.Nil;
        private DynValue _fourArgFunction = DynValue.Nil;
        private DynValue _fiveArgFunction = DynValue.Nil;
        private DynValue _coroutineFunction = DynValue.Nil;
        private DynValue _fourArgCoroutineFunction = DynValue.Nil;
        private DynValue _fiveArgCoroutineFunction = DynValue.Nil;
        private Closure _threeArgClosure;
        private Closure _fourArgClosure;
        private Closure _fiveArgClosure;
        private Coroutine _runningCoroutine;
        private Coroutine _fourArgRunningCoroutine;
        private Coroutine _fiveArgRunningCoroutine;
        private DynValue _first = DynValue.Nil;
        private DynValue _second = DynValue.Nil;
        private DynValue _third = DynValue.Nil;
        private DynValue _fourth = DynValue.Nil;
        private DynValue _fifth = DynValue.Nil;
        private DynValue[] _fiveDynValueArgs = Array.Empty<DynValue>();
        private DynValue[] _fiveDynValueArgsWithPadding = Array.Empty<DynValue>();
        private object _firstObject = 1d;
        private object _secondObject = 2d;
        private object _thirdObject = 3d;
        private object _fourthObject = 4d;
        private object _fifthObject = 5d;

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
            _threeArgClosure = _threeArgFunction.Function;
            _fourArgClosure = _fourArgFunction.Function;
            _fiveArgClosure = _fiveArgFunction.Function;
            _coroutineFunction = _script.DoString(
                "return function(a, b, c) while true do a, b, c = coroutine.yield(c) end end"
            );
            _fourArgCoroutineFunction = _script.DoString(
                "return function(a, b, c, d) while true do a, b, c, d = coroutine.yield(d) end end"
            );
            _fiveArgCoroutineFunction = _script.DoString(
                "return function(a, b, c, d, e) while true do a, b, c, d, e = coroutine.yield(e) end end"
            );
            _first = DynValue.NewNumber(1d);
            _second = DynValue.NewNumber(2d);
            _third = DynValue.NewNumber(3d);
            _fourth = DynValue.NewNumber(4d);
            _fifth = DynValue.NewNumber(5d);
            _fiveDynValueArgs = new[] { _first, _second, _third, _fourth, _fifth };
            _fiveDynValueArgsWithPadding = new[]
            {
                DynValue.Nil,
                _first,
                _second,
                _third,
                _fourth,
                _fifth,
                DynValue.Nil,
            };
            _firstObject = 1d;
            _secondObject = 2d;
            _thirdObject = 3d;
            _fourthObject = 4d;
            _fifthObject = 5d;
            _runningCoroutine = _script.CreateCoroutine(_coroutineFunction).Coroutine;
            _runningCoroutine.Resume(_first, _second, _third);
            _fourArgRunningCoroutine = _script.CreateCoroutine(_fourArgCoroutineFunction).Coroutine;
            _fourArgRunningCoroutine.Resume(_first, _second, _third, _fourth);
            _fiveArgRunningCoroutine = _script.CreateCoroutine(_fiveArgCoroutineFunction).Coroutine;
            _fiveArgRunningCoroutine.Resume(_first, _second, _third, _fourth, _fifth);
        }

        /// <summary>
        /// Calls a Lua closure with one pre-created DynValue argument.
        /// </summary>
        [Benchmark(Description = "Host Call: 1 DynValue")]
        public DynValue CallOneDynValue() => _script.Call(_oneArgFunction, _first);

        /// <summary>
        /// Calls a Lua closure with two pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 2 DynValues")]
        public DynValue CallTwoDynValues() => _script.Call(_twoArgFunction, _first, _second);

        /// <summary>
        /// Calls a Lua closure with three pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 3 DynValues")]
        public DynValue CallThreeDynValues() =>
            _script.Call(_threeArgFunction, _first, _second, _third);

        /// <summary>
        /// Calls a Lua closure with four pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 4 DynValues")]
        public DynValue CallFourDynValues() =>
            _script.Call(_fourArgFunction, _first, _second, _third, _fourth);

        /// <summary>
        /// Calls a Lua closure with five pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 5 DynValues")]
        public DynValue CallFiveDynValues() =>
            _script.Call(_fiveArgFunction, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a Lua closure through the params-array overload for comparison with fixed overloads.
        /// </summary>
        [Benchmark(Description = "Host Call: params 3 DynValues")]
        public DynValue CallThreeDynValuesParamsArray() =>
            _script.Call(_threeArgFunction, new DynValue[] { _first, _second, _third });

        /// <summary>
        /// Calls a Lua closure through the params-array overload for comparison with fixed overloads.
        /// </summary>
        [Benchmark(Description = "Host Call: params 4 DynValues")]
        public DynValue CallFourDynValuesParamsArray() =>
            _script.Call(_fourArgFunction, new DynValue[] { _first, _second, _third, _fourth });

        /// <summary>
        /// Calls a Lua closure through the params-array overload with five DynValues.
        /// </summary>
        [Benchmark(Description = "Host Call: params 5 DynValues")]
        public DynValue CallFiveDynValuesParamsArray() =>
            _script.Call(
                _fiveArgFunction,
                new DynValue[] { _first, _second, _third, _fourth, _fifth }
            );

        /// <summary>
        /// Calls a Lua closure with five pre-created DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "Host Call: span 5 DynValues")]
        public DynValue CallFiveDynValuesSpan() =>
            _script.Call(_fiveArgFunction, _fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a Lua closure with a slice of caller-owned contiguous DynValue storage.
        /// </summary>
        [Benchmark(Description = "Host Call: span slice 5 DynValues")]
        public DynValue CallFiveDynValuesSpanSlice() =>
            _script.Call(_fiveArgFunction, _fiveDynValueArgsWithPadding.AsSpan(1, 5));

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with three pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 3 DynValues")]
        public DynValue ClosureCallThreeDynValues() =>
            _threeArgClosure.Call(_first, _second, _third);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with four pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 4 DynValues")]
        public DynValue ClosureCallFourDynValues() =>
            _fourArgClosure.Call(_first, _second, _third, _fourth);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with five pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 5 DynValues")]
        public DynValue ClosureCallFiveDynValues() =>
            _fiveArgClosure.Call(_first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a Lua closure through the closure params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Closure Call: params 3 DynValues")]
        public DynValue ClosureCallThreeDynValuesParamsArray() =>
            _threeArgClosure.Call(new DynValue[] { _first, _second, _third });

        /// <summary>
        /// Calls a Lua closure through the closure params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Closure Call: params 4 DynValues")]
        public DynValue ClosureCallFourDynValuesParamsArray() =>
            _fourArgClosure.Call(new DynValue[] { _first, _second, _third, _fourth });

        /// <summary>
        /// Calls a Lua closure through the closure params-array overload with five DynValues.
        /// </summary>
        [Benchmark(Description = "Closure Call: params 5 DynValues")]
        public DynValue ClosureCallFiveDynValuesParamsArray() =>
            _fiveArgClosure.Call(new DynValue[] { _first, _second, _third, _fourth, _fifth });

        /// <summary>
        /// Calls a Lua closure through the closure span overload with five DynValues.
        /// </summary>
        [Benchmark(Description = "Closure Call: span 5 DynValues")]
        public DynValue ClosureCallFiveDynValuesSpan() =>
            _fiveArgClosure.Call(_fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a Lua closure with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 3 objects")]
        public DynValue CallThreeObjects() =>
            _script.Call(_threeArgFunction, _firstObject, _secondObject, _thirdObject);

        /// <summary>
        /// Calls a Lua closure with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: 4 objects")]
        public DynValue CallFourObjects() =>
            _script.Call(
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
        public DynValue CallFiveObjects() =>
            _script.Call(
                _fiveArgFunction,
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 3 objects")]
        public DynValue ClosureCallThreeObjects() =>
            _threeArgClosure.Call(_firstObject, _secondObject, _thirdObject);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 4 objects")]
        public DynValue ClosureCallFourObjects() =>
            _fourArgClosure.Call(_firstObject, _secondObject, _thirdObject, _fourthObject);

        /// <summary>
        /// Calls a Lua closure through the closure convenience API with five pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Closure Call: 5 objects")]
        public DynValue ClosureCallFiveObjects() =>
            _fiveArgClosure.Call(
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Calls a Lua closure through the object-function overload with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: closure object + 3 objects")]
        public DynValue CallClosureObjectThreeObjects() =>
            _script.Call(_threeArgClosure, _firstObject, _secondObject, _thirdObject);

        /// <summary>
        /// Calls a Lua closure through the object-function overload with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: closure object + 4 objects")]
        public DynValue CallClosureObjectFourObjects() =>
            _script.Call(_fourArgClosure, _firstObject, _secondObject, _thirdObject, _fourthObject);

        /// <summary>
        /// Calls a Lua closure through the object-function overload with five pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Host Call: closure object + 5 objects")]
        public DynValue CallClosureObjectFiveObjects() =>
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
        public DynValue CallThreeObjectsParamsArray() =>
            _script.Call(
                _threeArgFunction,
                new object[] { _firstObject, _secondObject, _thirdObject }
            );

        /// <summary>
        /// Calls a Lua closure through the object params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Host Call: params 4 objects")]
        public DynValue CallFourObjectsParamsArray() =>
            _script.Call(
                _fourArgFunction,
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine with three pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 3 DynValues")]
        public DynValue ResumeCoroutineThreeDynValues() =>
            _runningCoroutine.Resume(_first, _second, _third);

        /// <summary>
        /// Resumes a suspended Lua coroutine with four pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 4 DynValues")]
        public DynValue ResumeCoroutineFourDynValues() =>
            _fourArgRunningCoroutine.Resume(_first, _second, _third, _fourth);

        /// <summary>
        /// Resumes a suspended Lua coroutine with five pre-created DynValue arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 5 DynValues")]
        public DynValue ResumeCoroutineFiveDynValues() =>
            _fiveArgRunningCoroutine.Resume(_first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Resumes a suspended Lua coroutine with five pre-created DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: span 5 DynValues")]
        public DynValue ResumeCoroutineFiveDynValuesSpan() =>
            _fiveArgRunningCoroutine.Resume(_fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Resumes a suspended Lua coroutine with three pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 3 objects")]
        public DynValue ResumeCoroutineThreeObjects() =>
            _runningCoroutine.Resume(_firstObject, _secondObject, _thirdObject);

        /// <summary>
        /// Resumes a suspended Lua coroutine with four pre-created CLR object arguments.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: 4 objects")]
        public DynValue ResumeCoroutineFourObjects() =>
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
        public DynValue ResumeCoroutineFiveObjects() =>
            _fiveArgRunningCoroutine.Resume(
                _firstObject,
                _secondObject,
                _thirdObject,
                _fourthObject,
                _fifthObject
            );

        /// <summary>
        /// Resumes a suspended Lua coroutine through the params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 3 DynValues")]
        public DynValue ResumeCoroutineThreeDynValuesParamsArray() =>
            _runningCoroutine.Resume(new DynValue[] { _first, _second, _third });

        /// <summary>
        /// Resumes a suspended Lua coroutine through the params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 4 DynValues")]
        public DynValue ResumeCoroutineFourDynValuesParamsArray() =>
            _fourArgRunningCoroutine.Resume(new DynValue[] { _first, _second, _third, _fourth });

        /// <summary>
        /// Resumes a suspended Lua coroutine through the object params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 3 objects")]
        public DynValue ResumeCoroutineThreeObjectsParamsArray() =>
            _runningCoroutine.Resume(new object[] { _firstObject, _secondObject, _thirdObject });

        /// <summary>
        /// Resumes a suspended Lua coroutine through the object params-array overload for comparison.
        /// </summary>
        [Benchmark(Description = "Coroutine Suspended Resume: params 4 objects")]
        public DynValue ResumeCoroutineFourObjectsParamsArray() =>
            _fourArgRunningCoroutine.Resume(
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );
    }

    /// <summary>
    /// Benchmarks CLR object to <see cref="DynValue"/> conversion for common host API inputs.
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
            DynValue closure = _script.DoString("return function(value) return value end");
            _closureObject = closure.Function;
            _callbackObject = new CallbackFunction((_, _) => DynValue.Nil);
        }

        /// <summary>
        /// Converts a boxed integer to a Lua integer value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: int")]
        public DynValue FromInt() => DynValue.FromObject(_script, _intObject);

        /// <summary>
        /// Converts a boxed double to a Lua number value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: double")]
        public DynValue FromDouble() => DynValue.FromObject(_script, _doubleObject);

        /// <summary>
        /// Converts a boxed Boolean to a cached Lua Boolean value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: bool")]
        public DynValue FromBool() => DynValue.FromObject(_script, _boolObject);

        /// <summary>
        /// Converts a CLR string to a Lua string value.
        /// </summary>
        [Benchmark(Description = "Object Conversion: string")]
        public DynValue FromString() => DynValue.FromObject(_script, _stringObject);

        /// <summary>
        /// Converts a closure object through the cached wrapper path.
        /// </summary>
        [Benchmark(Description = "Object Conversion: closure")]
        public DynValue FromClosure() => DynValue.FromObject(_script, _closureObject);

        /// <summary>
        /// Converts a callback function object through the cached wrapper path.
        /// </summary>
        [Benchmark(Description = "Object Conversion: callback")]
        public DynValue FromCallback() => DynValue.FromObject(_script, _callbackObject);
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
        private DynValue _legacyCallback = DynValue.Nil;
        private DynValue _viewCallback = DynValue.Nil;
        private DynValue _first = DynValue.Nil;
        private DynValue _second = DynValue.Nil;
        private DynValue _third = DynValue.Nil;
        private DynValue _fourth = DynValue.Nil;
        private DynValue _fifth = DynValue.Nil;
        private DynValue[] _threeDynValueArgs = Array.Empty<DynValue>();
        private DynValue[] _fourDynValueArgs = Array.Empty<DynValue>();
        private DynValue[] _fiveDynValueArgs = Array.Empty<DynValue>();

        /// <summary>
        /// Prepares stable callback and argument values for CLR callback call benchmarks.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _legacyCallback = DynValue.NewCallback((_, args) => args[args.Count - 1]);
            _viewCallback = DynValue.NewCallbackView((_, args) => args[args.Count - 1]);
            _first = DynValue.NewNumber(1d);
            _second = DynValue.NewNumber(2d);
            _third = DynValue.NewNumber(3d);
            _fourth = DynValue.NewNumber(4d);
            _fifth = DynValue.NewNumber(5d);
            _threeDynValueArgs = new[] { _first, _second, _third };
            _fourDynValueArgs = new[] { _first, _second, _third, _fourth };
            _fiveDynValueArgs = new[] { _first, _second, _third, _fourth, _fifth };
        }

        /// <summary>
        /// Calls a legacy CLR callback with three fixed DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: 3 fixed DynValues")]
        public DynValue CallLegacyThreeDynValues() =>
            _script.Call(_legacyCallback, _first, _second, _third);

        /// <summary>
        /// Calls an argument-view CLR callback with three fixed DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: 3 fixed DynValues")]
        public DynValue CallViewThreeDynValues() =>
            _script.Call(_viewCallback, _first, _second, _third);

        /// <summary>
        /// Calls a legacy CLR callback through the params-array overload with three DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: params 3 DynValues")]
        public DynValue CallLegacyThreeDynValuesParamsArray() =>
            _script.Call(_legacyCallback, new DynValue[] { _first, _second, _third });

        /// <summary>
        /// Calls an argument-view CLR callback through the params-array overload with three DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: params 3 DynValues")]
        public DynValue CallViewThreeDynValuesParamsArray() =>
            _script.Call(_viewCallback, new DynValue[] { _first, _second, _third });

        /// <summary>
        /// Calls a legacy CLR callback with three DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: span 3 DynValues")]
        public DynValue CallLegacyThreeDynValuesSpan() =>
            _script.Call(_legacyCallback, _threeDynValueArgs.AsSpan());

        /// <summary>
        /// Calls an argument-view CLR callback with three DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: span 3 DynValues")]
        public DynValue CallViewThreeDynValuesSpan() =>
            _script.Call(_viewCallback, _threeDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a legacy CLR callback with four fixed DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: 4 fixed DynValues")]
        public DynValue CallLegacyFourDynValues() =>
            _script.Call(_legacyCallback, _first, _second, _third, _fourth);

        /// <summary>
        /// Calls an argument-view CLR callback with four fixed DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: 4 fixed DynValues")]
        public DynValue CallViewFourDynValues() =>
            _script.Call(_viewCallback, _first, _second, _third, _fourth);

        /// <summary>
        /// Calls a legacy CLR callback through the params-array overload with four DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: params 4 DynValues")]
        public DynValue CallLegacyFourDynValuesParamsArray() =>
            _script.Call(_legacyCallback, new DynValue[] { _first, _second, _third, _fourth });

        /// <summary>
        /// Calls an argument-view CLR callback through the params-array overload with four DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: params 4 DynValues")]
        public DynValue CallViewFourDynValuesParamsArray() =>
            _script.Call(_viewCallback, new DynValue[] { _first, _second, _third, _fourth });

        /// <summary>
        /// Calls a legacy CLR callback with four DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: span 4 DynValues")]
        public DynValue CallLegacyFourDynValuesSpan() =>
            _script.Call(_legacyCallback, _fourDynValueArgs.AsSpan());

        /// <summary>
        /// Calls an argument-view CLR callback with four DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: span 4 DynValues")]
        public DynValue CallViewFourDynValuesSpan() =>
            _script.Call(_viewCallback, _fourDynValueArgs.AsSpan());

        /// <summary>
        /// Calls a legacy CLR callback with five fixed DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: 5 fixed DynValues")]
        public DynValue CallLegacyFiveDynValues() =>
            _script.Call(_legacyCallback, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls an argument-view CLR callback with five fixed DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: 5 fixed DynValues")]
        public DynValue CallViewFiveDynValues() =>
            _script.Call(_viewCallback, _first, _second, _third, _fourth, _fifth);

        /// <summary>
        /// Calls a legacy CLR callback through the params-array overload with five DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: params 5 DynValues")]
        public DynValue CallLegacyFiveDynValuesParamsArray() =>
            _script.Call(
                _legacyCallback,
                new DynValue[] { _first, _second, _third, _fourth, _fifth }
            );

        /// <summary>
        /// Calls an argument-view CLR callback through the params-array overload with five DynValue arguments.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: params 5 DynValues")]
        public DynValue CallViewFiveDynValuesParamsArray() =>
            _script.Call(
                _viewCallback,
                new DynValue[] { _first, _second, _third, _fourth, _fifth }
            );

        /// <summary>
        /// Calls a legacy CLR callback with five DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback Legacy Call: span 5 DynValues")]
        public DynValue CallLegacyFiveDynValuesSpan() =>
            _script.Call(_legacyCallback, _fiveDynValueArgs.AsSpan());

        /// <summary>
        /// Calls an argument-view CLR callback with five DynValues from caller-owned contiguous storage.
        /// </summary>
        [Benchmark(Description = "CLR Callback View Call: span 5 DynValues")]
        public DynValue CallViewFiveDynValuesSpan() =>
            _script.Call(_viewCallback, _fiveDynValueArgs.AsSpan());
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
        private DynValue _pcallNoReturnFunction = DynValue.Nil;
        private DynValue _pcallOneReturnFunction = DynValue.Nil;
        private DynValue _tostringMetamethodFunction = DynValue.Nil;

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
        public DynValue PcallNoReturn() => _script.Call(_pcallNoReturnFunction);

        /// <summary>
        /// Executes a successful protected call whose callee returns one value.
        /// </summary>
        [Benchmark(Description = "Continuation: pcall one return")]
        public DynValue PcallOneReturn() => _script.Call(_pcallOneReturnFunction);

        /// <summary>
        /// Executes <c>tostring</c> through a table <c>__tostring</c> metamethod.
        /// </summary>
        [Benchmark(Description = "Continuation: tostring metamethod")]
        public DynValue TostringMetamethod() => _script.Call(_tostringMetamethodFunction);
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
        private DynValue _legacyThree = DynValue.Nil;
        private DynValue _viewThree = DynValue.Nil;
        private DynValue _legacyFour = DynValue.Nil;
        private DynValue _viewFour = DynValue.Nil;
        private DynValue _legacySpanProbeFour = DynValue.Nil;

        /// <summary>
        /// Prepares Lua closures that call CLR callbacks from bytecode.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _script.Globals["legacy"] = DynValue.NewCallback((_, args) => args[args.Count - 1]);
            _script.Globals["view"] = DynValue.NewCallbackView((_, args) => args[args.Count - 1]);
            _script.Globals["legacySpanProbe"] = DynValue.NewCallback(
                (_, args) =>
                    args.TryGetSpan(out ReadOnlySpan<DynValue> span)
                        ? span[span.Length - 1]
                        : args[args.Count - 1]
            );
            _legacyThree = _script.DoString("return function() return legacy(1, 2, 3) end");
            _viewThree = _script.DoString("return function() return view(1, 2, 3) end");
            _legacyFour = _script.DoString("return function() return legacy(1, 2, 3, 4) end");
            _viewFour = _script.DoString("return function() return view(1, 2, 3, 4) end");
            _legacySpanProbeFour = _script.DoString(
                "return function() return legacySpanProbe(1, 2, 3, 4) end"
            );
        }

        /// <summary>
        /// Runs Lua bytecode that calls a legacy CLR callback with three arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback Legacy: 3 args")]
        public DynValue CallLegacyThreeArgs() => _script.Call(_legacyThree);

        /// <summary>
        /// Runs Lua bytecode that calls an argument-view CLR callback with three arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View: 3 args")]
        public DynValue CallViewThreeArgs() => _script.Call(_viewThree);

        /// <summary>
        /// Runs Lua bytecode that calls a legacy CLR callback with four arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback Legacy: 4 args")]
        public DynValue CallLegacyFourArgs() => _script.Call(_legacyFour);

        /// <summary>
        /// Runs Lua bytecode that calls an argument-view CLR callback with four arguments.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback View: 4 args")]
        public DynValue CallViewFourArgs() => _script.Call(_viewFour);

        /// <summary>
        /// Runs Lua bytecode that calls a legacy CLR callback and consumes the VM-backed argument span.
        /// </summary>
        [Benchmark(Description = "Lua to CLR Callback Legacy TryGetSpan: 4 args")]
        public DynValue CallLegacySpanProbeFourArgs() => _script.Call(_legacySpanProbeFour);
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
        private DynValue _threeArgFunction = DynValue.Nil;
        private DynValue _fourArgFunction = DynValue.Nil;
        private DynValue _fiveArgFunction = DynValue.Nil;
        private DynValue _contextFixedThreeCallback = DynValue.Nil;
        private DynValue _contextParamsThreeCallback = DynValue.Nil;
        private DynValue _contextFixedFourCallback = DynValue.Nil;
        private DynValue _contextParamsFourCallback = DynValue.Nil;
        private DynValue _contextFixedFiveCallback = DynValue.Nil;
        private DynValue _contextParamsFiveCallback = DynValue.Nil;
        private DynValue _contextSpanFiveCallback = DynValue.Nil;
        private DynValue _first = DynValue.Nil;
        private DynValue _second = DynValue.Nil;
        private DynValue _third = DynValue.Nil;
        private DynValue _fourth = DynValue.Nil;
        private DynValue _fifth = DynValue.Nil;
        private DynValue[] _fiveDynValueArgs = Array.Empty<DynValue>();

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
            _first = DynValue.NewNumber(1d);
            _second = DynValue.NewNumber(2d);
            _third = DynValue.NewNumber(3d);
            _fourth = DynValue.NewNumber(4d);
            _fifth = DynValue.NewNumber(5d);
            _fiveDynValueArgs = new[] { _first, _second, _third, _fourth, _fifth };
            _contextFixedThreeCallback = DynValue.NewCallbackView(
                (context, _) => context.Call(_threeArgFunction, _first, _second, _third)
            );
            _contextParamsThreeCallback = DynValue.NewCallbackView(
                (context, _) =>
                    context.Call(_threeArgFunction, new DynValue[] { _first, _second, _third })
            );
            _contextFixedFourCallback = DynValue.NewCallbackView(
                (context, _) => context.Call(_fourArgFunction, _first, _second, _third, _fourth)
            );
            _contextParamsFourCallback = DynValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        _fourArgFunction,
                        new DynValue[] { _first, _second, _third, _fourth }
                    )
            );
            _contextFixedFiveCallback = DynValue.NewCallbackView(
                (context, _) =>
                    context.Call(_fiveArgFunction, _first, _second, _third, _fourth, _fifth)
            );
            _contextParamsFiveCallback = DynValue.NewCallbackView(
                (context, _) =>
                    context.Call(
                        _fiveArgFunction,
                        new DynValue[] { _first, _second, _third, _fourth, _fifth }
                    )
            );
            _contextSpanFiveCallback = DynValue.NewCallbackView(
                (context, _) => context.Call(_fiveArgFunction, _fiveDynValueArgs.AsSpan())
            );
        }

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed three-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 3 fixed DynValues")]
        public DynValue CallContextThreeDynValues() => _script.Call(_contextFixedThreeCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the params-array context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: params 3 DynValues")]
        public DynValue CallContextThreeDynValuesParamsArray() =>
            _script.Call(_contextParamsThreeCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed four-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 4 fixed DynValues")]
        public DynValue CallContextFourDynValues() => _script.Call(_contextFixedFourCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the params-array context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: params 4 DynValues")]
        public DynValue CallContextFourDynValuesParamsArray() =>
            _script.Call(_contextParamsFourCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the fixed five-argument context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: 5 fixed DynValues")]
        public DynValue CallContextFiveDynValues() => _script.Call(_contextFixedFiveCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the params-array context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: params 5 DynValues")]
        public DynValue CallContextFiveDynValuesParamsArray() =>
            _script.Call(_contextParamsFiveCallback);

        /// <summary>
        /// Calls back into Lua from a CLR callback through the span context overload.
        /// </summary>
        [Benchmark(Description = "Context Call: span 5 DynValues")]
        public DynValue CallContextFiveDynValuesSpan() => _script.Call(_contextSpanFiveCallback);
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
        private DynValue _value = DynValue.Nil;

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
            _value = DynValue.NewNumber(42);

            _table.Set("child", DynValue.NewTable(child));
            child.Set("grandchild", DynValue.NewTable(grandchild));
            child.Set("leaf", _value);
            grandchild.Set("leaf", _value);
            _twoKeys = new object[] { "child", "leaf" };
            _threeKeys = new object[] { "child", "grandchild", "leaf" };
        }

        /// <summary>
        /// Reads a nested value through the fixed two-key raw lookup overload.
        /// </summary>
        [Benchmark(Description = "Table RawGet: 2 fixed keys")]
        public DynValue RawGetTwoFixedKeys() => _table.RawGet("child", "leaf");

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table RawGet: array 2 keys")]
        public DynValue RawGetTwoArrayKeys() => _table.RawGet(_twoKeys);

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Table RawGet: new array 2 keys")]
        public DynValue RawGetTwoNewArrayKeys() => _table.RawGet(new object[] { "child", "leaf" });

        /// <summary>
        /// Reads a nested value through the fixed three-key raw lookup overload.
        /// </summary>
        [Benchmark(Description = "Table RawGet: 3 fixed keys")]
        public DynValue RawGetThreeFixedKeys() => _table.RawGet("child", "grandchild", "leaf");

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with a stable key buffer.
        /// </summary>
        [Benchmark(Description = "Table RawGet: array 3 keys")]
        public DynValue RawGetThreeArrayKeys() => _table.RawGet(_threeKeys);

        /// <summary>
        /// Reads a nested value through the array-backed raw lookup overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Table RawGet: new array 3 keys")]
        public DynValue RawGetThreeNewArrayKeys() =>
            _table.RawGet(new object[] { "child", "grandchild", "leaf" });

        /// <summary>
        /// Reads a nested value through the fixed two-key lookup overload.
        /// </summary>
        [Benchmark(Description = "Table Get: 2 fixed keys")]
        public DynValue GetTwoFixedKeys() => _table.Get("child", "leaf");

        /// <summary>
        /// Reads a nested value through the array-backed lookup overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Table Get: new array 2 keys")]
        public DynValue GetTwoNewArrayKeys() => _table.Get(new object[] { "child", "leaf" });

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
        public DynValue CallFunctionTwoFixedObjects() =>
            _mod.CallFunction("second", _firstObject, _secondObject);

        /// <summary>
        /// Calls a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: params 2 objects")]
        public DynValue CallFunctionTwoParamsArray() =>
            _mod.CallFunction("second", new object[] { _firstObject, _secondObject });

        /// <summary>
        /// Calls a mod function through the fixed four-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: 4 fixed objects")]
        public DynValue CallFunctionFourFixedObjects() =>
            _mod.CallFunction("fourth", _firstObject, _secondObject, _thirdObject, _fourthObject);

        /// <summary>
        /// Calls a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: params 4 objects")]
        public DynValue CallFunctionFourParamsArray() =>
            _mod.CallFunction(
                "fourth",
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );

        /// <summary>
        /// Calls a mod function through the fixed five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod CallFunction: 5 fixed objects")]
        public DynValue CallFunctionFiveFixedObjects() =>
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
        public DynValue CallFunctionFiveParamsArray() =>
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
        /// Broadcasts a mod function through the fixed two-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: 2 fixed objects")]
        public IDictionary<string, DynValue> BroadcastCallTwoFixedObjects() =>
            _manager.BroadcastCall("second", _firstObject, _secondObject);

        /// <summary>
        /// Broadcasts a mod function through the params-array overload with caller allocation.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: params 2 objects")]
        public IDictionary<string, DynValue> BroadcastCallTwoParamsArray() =>
            _manager.BroadcastCall("second", new object[] { _firstObject, _secondObject });

        /// <summary>
        /// Broadcasts a mod function through the fixed four-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: 4 fixed objects")]
        public IDictionary<string, DynValue> BroadcastCallFourFixedObjects() =>
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
        public IDictionary<string, DynValue> BroadcastCallFourParamsArray() =>
            _manager.BroadcastCall(
                "fourth",
                new object[] { _firstObject, _secondObject, _thirdObject, _fourthObject }
            );

        /// <summary>
        /// Broadcasts a mod function through the fixed five-argument overload.
        /// </summary>
        [Benchmark(Description = "Mod BroadcastCall: 5 fixed objects")]
        public IDictionary<string, DynValue> BroadcastCallFiveFixedObjects() =>
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
        public IDictionary<string, DynValue> BroadcastCallFiveParamsArray() =>
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
    }
}
