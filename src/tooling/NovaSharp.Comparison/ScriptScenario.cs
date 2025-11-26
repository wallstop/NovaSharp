namespace NovaSharp.Comparison;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Benchmark scenarios used when comparing NovaSharp against NLua.
/// </summary>
[SuppressMessage(
    "Usage",
    "CA1515:Consider making public types internal",
    Justification = "BenchmarkDotNet requires public enums for [Params] discovery."
)]
public enum ScriptScenario
{
    [Obsolete("Use a specific ScriptScenario.", false)]
    Unknown = 0,
    TowerOfHanoi = 1,
    EightQueens = 2,
    CoroutinePingPong = 3,
}
