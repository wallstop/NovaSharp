namespace NovaSharp.Benchmarks
{
    using System;

    /// <summary>
    /// High-level scenarios exercised by the NovaSharp runtime benchmarks.
    /// </summary>
    public enum RuntimeScenario
    {
        [Obsolete("Use a specific RuntimeScenario.", false)]
        Unknown = 0,
        NumericLoops = 1,
        TableMutation = 2,
        CoroutinePipeline = 3,
        UserDataInterop = 4,
    }

    /// <summary>
    /// Provides ready-to-run Lua scripts that stress specific interpreter behaviors.
    /// </summary>
    internal static class LuaRuntimeSuites
    {
        public const int LoopIterations = 2_000;
        public const int TableEntryCount = 128;
        public const int CoroutineSteps = 64;
        public const int UserDataIterations = 256;

        /// <summary>
        /// Returns the Lua script associated with the requested <paramref name="scenario"/>.
        /// </summary>
        /// <param name="scenario">Scenario enumerating the runtime behavior to benchmark.</param>
        /// <returns>Lua script text suitable for <c>Script.DoString</c>.</returns>
        public static string GetScript(RuntimeScenario scenario) =>
            scenario switch
            {
                RuntimeScenario.NumericLoops => NumericLoopScript,
                RuntimeScenario.TableMutation => TableMutationScript,
                RuntimeScenario.CoroutinePipeline => CoroutinePipelineScript,
                RuntimeScenario.UserDataInterop => UserDataInteropScript,
                _ => NumericLoopScript,
            };

        private static readonly string NumericLoopScript =
            $@"
return function ()
    local sum = 0.0
    for i = 1, {LoopIterations} do
        sum = sum + math.sin(i) * math.cos(i * 0.5)
        if (i % 7) == 0 then
            sum = sum / 2.0
        end
    end
    return sum
end";

        private const string TableMutationScript =
            $@"
return function (source)
    local acc = 0
    for i = 1, #source do
        acc = acc + source[i]
        source[i] = acc % 17
    end
    for k = #source, 1, -3 do
        source[k] = nil
    end
    return acc
end";

        private const string CoroutinePipelineScript =
            $@"
return function (steps)
    local producer = coroutine.create(function(n)
        local value = 0
        for i = 1, n do
            value = value + math.sqrt(i)
            coroutine.yield(value)
        end
        return value
    end)

    local last = 0
    for i = 1, steps do
        local ok, result = coroutine.resume(producer, i + (i % 3))
        if not ok then error(result) end
        last = result
    end
    return last
end";

        private const string UserDataInteropScript =
            $@"
return function (host, iterations)
    local value = 0
    for i = 1, iterations do
        value = value + host:Accumulate(i, i * 2)
    end
    host:Store(value)
    return value
end";
    }
}
