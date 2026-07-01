namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Provides an allocation-conscious mod function call path for containers that can consume
    /// caller-owned object argument spans.
    /// </summary>
    public interface IModContainerObjectArguments
    {
        /// <summary>
        /// Invokes a global function defined in this mod with caller-owned CLR object arguments.
        /// </summary>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="args">Arguments to pass to the function.</param>
        /// <returns>The result of the function call.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the mod is not loaded.</exception>
        /// <remarks>
        /// The span is always interpreted as the function's argument list. To pass an
        /// <see cref="object" /> array as a single Lua argument, use the existing
        /// <c>CallFunction(functionName, (object)array)</c> call shape.
        /// </remarks>
        public DynValue CallFunctionObjectArguments(string functionName, ReadOnlySpan<object> args);
    }
}
