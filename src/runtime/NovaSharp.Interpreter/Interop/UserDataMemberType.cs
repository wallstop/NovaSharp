namespace NovaSharp.Interpreter.Interop
{
    using System;
    using NovaSharp.Interpreter.DataTypes;

    public enum UserDataMemberType
    {
        [Obsolete("Use a specific UserDataMemberType.", false)]
        Unknown = 0,
        Constructor = 1,
        Method = 2,
        Property = 3,
        Field = 4,
        Event = 5,
    }
}
