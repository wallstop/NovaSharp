namespace WallstopStudios.NovaSharp.Comparison;

using System;

/// <summary>
/// Benchmark scenarios used when comparing NovaSharp against other Lua runtimes.
/// </summary>
internal enum ScriptScenario
{
    [Obsolete("Use a specific ScriptScenario.", false)]
    Unknown = 0,
    NumericLoops = 1,
    TableMutation = 2,
    TowerOfHanoi = 3,
    EightQueens = 4,
    CoroutinePingPong = 5,
}
