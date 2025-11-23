namespace NovaSharp.Comparison;

using System;

internal enum ScriptScenario
{
    [Obsolete("Use a specific ScriptScenario.", false)]
    Unknown = 0,
    TowerOfHanoi = 1,
    EightQueens = 2,
    CoroutinePingPong = 3,
}
