namespace WallstopStudios.NovaSharp.Interpreter.DataTypes
{
    /// <summary>
    /// Represents a callable wrapper around a NovaSharp script function that returns <see cref="object" />.
    /// </summary>
    /// <param name="args">Arguments that should be passed to the Lua closure.</param>
    /// <returns>The Lua return value projected back into CLR space.</returns>
    public delegate object ScriptFunctionCallback(params object[] args);

    /// <summary>
    /// Represents a callable wrapper around a NovaSharp script function that returns a strongly typed result.
    /// </summary>
    /// <typeparam name="T">CLR return type requested by the caller.</typeparam>
    /// <param name="args">Arguments that should be passed to the Lua closure.</param>
    /// <returns>The Lua return value projected into <typeparamref name="T" />.</returns>
    public delegate T ScriptFunctionCallback<T>(params object[] args);
}
