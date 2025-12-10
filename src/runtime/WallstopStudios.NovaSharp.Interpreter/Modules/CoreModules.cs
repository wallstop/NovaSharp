namespace WallstopStudios.NovaSharp.Interpreter.Modules
{
    using System;

    /// <summary>
    /// Enumeration (combinable as flags) of all the standard library modules.
    /// For preset combinations, use the <see cref="CoreModulePresets"/> class instead of the legacy enum members.
    /// </summary>
    [Flags]
    public enum CoreModules
    {
        /// <summary>
        /// Value used to specify no modules to be loaded (equals 0).
        /// </summary>
        [Obsolete("Prefer explicit CoreModules combinations.", false)]
        None = 0,

        /// <summary>
        /// The basic methods. Includes "assert", "collectgarbage", "error", "print", "select", "type", "tonumber" and "tostring".
        /// </summary>
        Basic = 1 << 6,

        /// <summary>
        /// The global constants: "_G", "_VERSION" and "_NovaSharp".
        /// </summary>
        GlobalConsts = 1 << 0,

        /// <summary>
        /// The table iterators: "next", "ipairs" and "pairs".
        /// </summary>
        TableIterators = 1 << 1,

        /// <summary>
        /// The metatable methods : "setmetatable", "getmetatable", "rawset", "rawget", "rawequal" and "rawlen".
        /// </summary>
        Metatables = 1 << 2,

        /// <summary>
        /// The string package
        /// </summary>
        StringLib = 1 << 3,

        /// <summary>
        /// The load methods: "load", "loadsafe", "loadfile", "loadfilesafe", "dofile" and "require"
        /// </summary>
        LoadMethods = 1 << 4,

        /// <summary>
        /// The table package
        /// </summary>
        Table = 1 << 5,

        /// <summary>
        /// The error handling methods: "pcall" and "xpcall"
        /// </summary>
        ErrorHandling = 1 << 7,

        /// <summary>
        /// The math package
        /// </summary>
        Math = 1 << 8,

        /// <summary>
        /// The coroutine package
        /// </summary>
        Coroutine = 1 << 9,

        /// <summary>
        /// The bit32 package
        /// </summary>
        Bit32 = 1 << 10,

        /// <summary>
        /// The time methods of the "os" package: "clock", "difftime", "date" and "time"
        /// </summary>
        OsTime = 1 << 11,

        /// <summary>
        /// The methods of "os" package excluding those listed for OsTime. These are not supported under Unity.
        /// </summary>
        OsSystem = 1 << 12,

        /// <summary>
        /// The methods of "io" and "file" packages. These are not supported under Unity.
        /// </summary>
        Io = 1 << 13,

        /// <summary>
        /// The "debug" package (it has limited support)
        /// </summary>
        Debug = 1 << 14,

        /// <summary>
        /// The "dynamic" package (introduced by NovaSharp).
        /// </summary>
        Dynamic = 1 << 15,

        /// <summary>
        /// The "json" package (introduced by NovaSharp).
        /// </summary>
        Json = 1 << 16,
    }

    /// <summary>
    /// Preset combinations of <see cref="CoreModules"/> flags.
    /// These are the recommended way to specify module sets when creating a <see cref="Script"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per best practices, combined flag values are defined as static readonly fields in this helper class
    /// rather than as enum members. This ensures:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Semantic clarity: each enum member represents a single flag</description></item>
    /// <item><description>Serialization safety: combined values don't pollute serialized data</description></item>
    /// <item><description>Reflection predictability: <see cref="Enum.GetValues(Type)"/> returns only single-bit values</description></item>
    /// <item><description>Maintainability: adding new flags doesn't require updating combined value members</description></item>
    /// </list>
    /// </remarks>
    public static class CoreModulePresets
    {
        /// <summary>
        /// A sort of "hard" sandbox preset, including string, math, table, bit32 packages, constants and table iterators.
        /// </summary>
        /// <remarks>
        /// Includes: <see cref="CoreModules.GlobalConsts"/>, <see cref="CoreModules.TableIterators"/>,
        /// <see cref="CoreModules.StringLib"/>, <see cref="CoreModules.Table"/>, <see cref="CoreModules.Basic"/>,
        /// <see cref="CoreModules.Math"/>, <see cref="CoreModules.Bit32"/>.
        /// </remarks>
        public static readonly CoreModules HardSandbox =
            CoreModules.GlobalConsts
            | CoreModules.TableIterators
            | CoreModules.StringLib
            | CoreModules.Table
            | CoreModules.Basic
            | CoreModules.Math
            | CoreModules.Bit32;

        /// <summary>
        /// A softer sandbox preset, adding metatables support, error handling, coroutine, time functions, json parsing and dynamic evaluations.
        /// </summary>
        /// <remarks>
        /// Includes everything in <see cref="HardSandbox"/> plus: <see cref="CoreModules.Metatables"/>,
        /// <see cref="CoreModules.ErrorHandling"/>, <see cref="CoreModules.Coroutine"/>, <see cref="CoreModules.OsTime"/>,
        /// <see cref="CoreModules.Dynamic"/>, <see cref="CoreModules.Json"/>.
        /// </remarks>
        public static readonly CoreModules SoftSandbox =
            HardSandbox
            | CoreModules.Metatables
            | CoreModules.ErrorHandling
            | CoreModules.Coroutine
            | CoreModules.OsTime
            | CoreModules.Dynamic
            | CoreModules.Json;

        /// <summary>
        /// The default preset. Includes everything except "debug".
        /// </summary>
        /// <remarks>
        /// <para>
        /// Beware that using this preset allows scripts unlimited access to the system.
        /// </para>
        /// <para>
        /// Includes everything in <see cref="SoftSandbox"/> plus: <see cref="CoreModules.LoadMethods"/>,
        /// <see cref="CoreModules.OsSystem"/>, <see cref="CoreModules.Io"/>.
        /// </para>
        /// </remarks>
        public static readonly CoreModules Default =
            SoftSandbox | CoreModules.LoadMethods | CoreModules.OsSystem | CoreModules.Io;

        /// <summary>
        /// The complete package, including all modules.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Beware that using this preset allows scripts unlimited access to the system.
        /// </para>
        /// <para>
        /// Includes everything in <see cref="Default"/> plus: <see cref="CoreModules.Debug"/>.
        /// </para>
        /// </remarks>
        public static readonly CoreModules Complete = Default | CoreModules.Debug;
    }

    /// <summary>
    /// Extension helpers for interrogating <see cref="CoreModules"/> flag values.
    /// </summary>
    internal static class CoreModulesExtensionMethods
    {
        /// <summary>
        /// Returns <c>true</c> when <paramref name="val"/> contains every bit set in <paramref name="flag"/>.
        /// </summary>
        public static bool Has(this CoreModules val, CoreModules flag)
        {
            return (val & flag) == flag;
        }
    }
}
