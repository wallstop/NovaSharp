namespace WallstopStudios.NovaSharp.Unity.Samples
{
    using System;
    using System.Globalization;
    using UnityEngine;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using Stopwatch = System.Diagnostics.Stopwatch;

    /// <summary>
    /// Runs a minimal NovaSharp workload intended for IL2CPP player smoke checks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IL2CPPSpotCheckRunner : MonoBehaviour
    {
        private const string PassPrefix = "NOVASHARP_IL2CPP_SPOTCHECK PASS";
        private const string FailPrefix = "NOVASHARP_IL2CPP_SPOTCHECK FAIL";

        private const string BenchmarkScript =
            @"
local tableValue = {}
local counter = 0

function nova_tick()
    counter = counter + 1
    return counter
end

function nova_table()
    local key = (counter % 64) + 1
    tableValue[key] = (tableValue[key] or 0) + 1
    return tableValue[key]
end

function nova_callback()
    return host_add(counter, 7)
end
";

        [SerializeField]
        private bool _runOnStart = true;

        [SerializeField]
        private int _warmupCount = 256;

        [SerializeField]
        private int _iterationCount = 5000;

        [SerializeField]
        private bool _runClrCallbackCheck = true;

        private Script _script;
        private DynValue _tickFunction;
        private DynValue _tableFunction;
        private DynValue _callbackFunction;

        private void Start()
        {
            if (_runOnStart)
            {
                RunSpotCheck();
            }
        }

        /// <summary>
        /// Executes the spot check and logs one machine-readable pass or fail line.
        /// </summary>
        [ContextMenu("Run NovaSharp IL2CPP Spot Check")]
        public void RunSpotCheck()
        {
            try
            {
                EnsureScript();

                int warmups = Math.Max(0, _warmupCount);
                for (int i = 0; i < warmups; i++)
                {
                    RunOneIteration();
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                long managedBytesBefore = GC.GetTotalMemory(false);
                int iterations = Math.Max(1, _iterationCount);
                double checksum = 0;
                Stopwatch stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < iterations; i++)
                {
                    checksum += RunOneIteration();
                }
                stopwatch.Stop();
                long managedBytesAfter = GC.GetTotalMemory(false);

                int callsPerIteration = _runClrCallbackCheck ? 3 : 2;
                long callCount = (long)iterations * callsPerIteration;
                double nanosecondsPerCall =
                    stopwatch.Elapsed.TotalMilliseconds * 1000000.0 / callCount;

                Debug.Log(
                    string.Concat(
                        PassPrefix,
                        " iterations=",
                        iterations.ToString(CultureInfo.InvariantCulture),
                        " calls=",
                        callCount.ToString(CultureInfo.InvariantCulture),
                        " elapsedMs=",
                        stopwatch.Elapsed.TotalMilliseconds.ToString(
                            "F3",
                            CultureInfo.InvariantCulture
                        ),
                        " nsPerCall=",
                        nanosecondsPerCall.ToString("F1", CultureInfo.InvariantCulture),
                        " managedBytesDelta=",
                        (managedBytesAfter - managedBytesBefore).ToString(
                            CultureInfo.InvariantCulture
                        ),
                        " checksum=",
                        checksum.ToString("F0", CultureInfo.InvariantCulture)
                    )
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(FormatFailure(ex));
            }
        }

        private static string FormatFailure(Exception exception)
        {
            string exceptionType = exception.GetType().FullName ?? exception.GetType().Name;
            string message = ToSingleLogLine(exception.Message);
            if (string.IsNullOrEmpty(message))
            {
                message = "<no-message>";
            }

            return string.Concat(
                FailPrefix,
                " errorType=",
                exceptionType,
                " message=",
                message
            );
        }

        private static string ToSingleLogLine(string value)
        {
            return value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        private void EnsureScript()
        {
            if (_script != null)
            {
                return;
            }

            _script = new Script(CoreModulePresets.Complete);
            _script.Globals.Set("host_add", DynValue.NewCallback(HostAdd, "host_add"));
            _script.DoString(BenchmarkScript, null, "NovaSharpIL2CPPSpotCheck");
            _tickFunction = _script.Globals.Get("nova_tick");
            _tableFunction = _script.Globals.Get("nova_table");
            _callbackFunction = _script.Globals.Get("nova_callback");
        }

        private double RunOneIteration()
        {
            DynValue tickResult = _script.Call(_tickFunction);
            DynValue tableResult = _script.Call(_tableFunction);
            if (!_runClrCallbackCheck)
            {
                return tickResult.Number + tableResult.Number;
            }

            DynValue callbackResult = _script.Call(_callbackFunction);
            return tickResult.Number + tableResult.Number + callbackResult.Number;
        }

        private static DynValue HostAdd(ScriptExecutionContext context, CallbackArguments args)
        {
            int left = args.AsInt(0, "host_add");
            int right = args.AsInt(1, "host_add");
            return DynValue.NewNumber(left + right);
        }
    }
}
