namespace WallstopStudios.NovaSharp.Interpreter.Serialization.Json
{
    using System;
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
        public static bool IsJsonNull(DynValue v)
        {
            if (v == null)
            {
                throw new ArgumentNullException(nameof(v));
            }

            return v.Type == DataType.UserData
                && v.UserData.Descriptor != null
                && v.UserData.Descriptor.Type == typeof(JsonNull);
        }

        [NovaSharpHidden]
        /// <summary>
        /// Creates a userdata instance representing JSON null.
        /// </summary>
        public static DynValue Create()
        {
            return UserData.CreateStatic<JsonNull>();
        }
    }
}
