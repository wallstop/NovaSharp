namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Test-only helpers for selecting consistent CoreModules combinations.
    /// </summary>
    internal static class TestCoreModules
    {
        /// <summary>
        /// Minimal set of modules that keeps the global constants and base helpers available.
        /// </summary>
        public const CoreModules BasicGlobals = CoreModules.Basic | CoreModules.GlobalConsts;
    }
}
