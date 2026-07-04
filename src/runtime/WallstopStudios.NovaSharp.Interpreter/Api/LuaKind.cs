namespace NovaSharp
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Public Lua value kind exposed by the facade API.
    /// </summary>
    [SuppressMessage(
        "Naming",
        "CA1720:Identifier contains type name",
        Justification = "Lua kind names intentionally mirror Lua type and numeric subtype terminology."
    )]
    public enum LuaKind
    {
        Nil = 0,
        Boolean = 1,
        Integer = 2,
        Float = 3,
        String = 4,
        Function = 5,
        Table = 6,
        Tuple = 7,
        UserData = 8,
        Thread = 9,
    }
}
