namespace NovaSharp
{
    using System;

    /// <summary>
    /// Standard library modules registered in a facade engine.
    /// </summary>
    [Flags]
    public enum LuaCoreModules
    {
        None = 0,
        GlobalConsts = 1 << 0,
        TableIterators = 1 << 1,
        Metatables = 1 << 2,
        StringLib = 1 << 3,
        LoadMethods = 1 << 4,
        Table = 1 << 5,
        Basic = 1 << 6,
        ErrorHandling = 1 << 7,
        Math = 1 << 8,
        Coroutine = 1 << 9,
        Bit32 = 1 << 10,
        OsTime = 1 << 11,
        OsSystem = 1 << 12,
        Io = 1 << 13,
        Debug = 1 << 14,
        Dynamic = 1 << 15,
        Json = 1 << 16,
    }
}
