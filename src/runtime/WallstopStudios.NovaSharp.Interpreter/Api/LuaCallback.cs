namespace NovaSharp
{
    using System;

    /// <summary>
    /// Represents a host callback exposed through the root Lua facade.
    /// </summary>
    /// <param name="context">The callback execution context.</param>
    /// <param name="args">The Lua arguments supplied by the script.</param>
    /// <returns>The Lua value returned by the callback.</returns>
    public delegate LuaValue LuaCallback(LuaContext context, ReadOnlySpan<LuaValue> args);
}
