namespace NovaSharp.Interpreter.Serialization.Json
{
    /// <summary>
    /// UserData representing a null value in a table converted from Json
    /// </summary>
    public sealed class JsonNull
    {
        public static bool IsNull()
        {
            return true;
        }

        [NovaSharpHidden]
        public static bool IsJsonNull(DynValue v)
        {
            return v.Type == DataType.UserData
                && v.UserData.Descriptor != null
                && v.UserData.Descriptor.Type == typeof(JsonNull);
        }

        [NovaSharpHidden]
        public static DynValue Create()
        {
            return UserData.CreateStatic<JsonNull>();
        }
    }
}
