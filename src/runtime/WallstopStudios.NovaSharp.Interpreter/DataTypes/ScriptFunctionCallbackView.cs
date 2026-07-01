namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    using WallstopStudios.NovaSharp.Interpreter.Execution;

    /// <summary>
    /// Represents an opt-in CLR callback that receives a stack-only argument view.
    /// </summary>
    /// <param name="executionContext">The current script execution context.</param>
    /// <param name="args">The callback arguments.</param>
    /// <returns>The Lua value returned by the callback.</returns>
    public delegate DynValue ScriptFunctionCallbackView(
        ScriptExecutionContext executionContext,
        CallbackArgumentsView args
    );

    /// <summary>
    /// Represents an opt-in CLR callback that receives a stack-only argument view without requiring
    /// a <see cref="ScriptExecutionContext"/>.
    /// </summary>
    /// <param name="args">The callback arguments.</param>
    /// <returns>The Lua value returned by the callback.</returns>
    public delegate DynValue ScriptFunctionCallbackViewNoContext(CallbackArgumentsView args);
}
