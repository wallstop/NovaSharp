namespace WallstopStudios.NovaSharp.Interpreter.Serialization.Json
{
    using System;
    using global::NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Interop.Attributes;

    /// <summary>
    /// UserData representing a null value in a table converted from JSON.
    /// </summary>
    public sealed class JsonNull
    {
        /// <summary>
        /// Returns <c>true</c> so consumers can treat <see cref="JsonNull"/> instances like Lua <c>nil</c>.
        /// </summary>
        public static bool IsNull()
        {
            return true;
        }

        [NovaSharpHidden]
        /// <summary>
        /// Detects whether the supplied value wraps the <see cref="JsonNull"/> userdata sentinel.
        /// </summary>
        public static bool IsJsonNull(LuaValue v)
        {
            return v.Type == DataType.UserData
                && v.UserData.Descriptor != null
                && v.UserData.Descriptor.Type == typeof(JsonNull);
        }

        [NovaSharpHidden]
        /// <summary>
        /// Creates a userdata instance representing JSON null.
        /// </summary>
        public static LuaValue Create()
        {
            if (!UserData.TryCreateStatic<JsonNull>(out LuaValue value))
            {
                throw new InvalidOperationException("Failed to create JSON null userdata.");
            }

            return value;
        }
    }
}
