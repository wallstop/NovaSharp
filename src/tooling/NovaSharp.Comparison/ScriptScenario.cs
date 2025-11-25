namespace NovaSharp.Comparison;

using System;

/// <summary>
/// Benchmark scenarios used when comparing NovaSharp against NLua.
/// </summary>
public enum ScriptScenario
{
    [Obsolete("Use a specific ScriptScenario.", false)]
    Unknown = 0,
    TowerOfHanoi = 1,
    EightQueens = 2,
    CoroutinePingPong = 3,
}
