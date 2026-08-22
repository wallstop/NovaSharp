namespace WallstopStudios.NovaSharp.Unity.Samples
{
    using System;
    using System.Globalization;
    using global::NovaSharp;
    using UnityEngine;
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

        private LuaEngine _lua;
        private LuaFunction _tickFunction;
        private LuaFunction _tableFunction;
        private LuaFunction _callbackFunction;

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
                Debug.Log(FormatFailure(ex));
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

            return string.Concat(FailPrefix, " errorType=", exceptionType, " message=", message);
        }

        private static string ToSingleLogLine(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }

        private void EnsureScript()
        {
            if (_lua != null)
            {
                return;
            }

            LuaEngine lua = LuaEngine.Create();
            lua.Globals.Set("host_add", lua.CreateCallback(HostAdd, "host_add"));
            lua.Run(BenchmarkScript, "NovaSharpIL2CPPSpotCheck");

            LuaFunction tickFunction = lua.Globals.Get("nova_tick").AsFunction();
            LuaFunction tableFunction = lua.Globals.Get("nova_table").AsFunction();
            LuaFunction callbackFunction = lua.Globals.Get("nova_callback").AsFunction();

            _tickFunction = tickFunction;
            _tableFunction = tableFunction;
            _callbackFunction = callbackFunction;
            _lua = lua;
        }

        private double RunOneIteration()
        {
            LuaValue tickResult = _tickFunction.Call();
            LuaValue tableResult = _tableFunction.Call();
            if (!_runClrCallbackCheck)
            {
                return tickResult.AsNumber() + tableResult.AsNumber();
            }

            LuaValue callbackResult = _callbackFunction.Call();
            return tickResult.AsNumber() + tableResult.AsNumber() + callbackResult.AsNumber();
        }

        private static LuaValue HostAdd(LuaContext context, ReadOnlySpan<LuaValue> args)
        {
            long left = args[0].AsInteger();
            long right = args[1].AsInteger();
            return LuaValue.FromInteger(left + right);
        }

        private void OnDestroy()
        {
            _lua?.Dispose();
        }
    }
}
