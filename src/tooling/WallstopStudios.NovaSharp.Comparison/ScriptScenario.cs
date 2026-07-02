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
    FibonacciRecursive = 6,
    NBody = 7,
    BinaryTrees = 8,
    SpectralNorm = 9,
    TableIntegerFillIterate = 10,
    TableStringKeyLookup = 11,
    TableNextTraversal = 12,
    TableInsertRemoveChurn = 13,
    StringConcatChains = 14,
    StringPatternGsubFind = 15,
    StringFormat = 16,
}
