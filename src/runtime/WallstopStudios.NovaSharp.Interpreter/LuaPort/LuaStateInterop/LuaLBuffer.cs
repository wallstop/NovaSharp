// Disable warnings about XML documentation
namespace WallstopStudios.NovaSharp.Interpreter.LuaPort.LuaStateInterop
{
#pragma warning disable IDE1006 // Mirrors upstream Lua C API naming (snake_case preserved intentionally).

    using System.Text;

    public class LuaLBuffer
    {
        public StringBuilder StringBuilder { get; private set; }
        public LuaState LuaState { get; private set; }

        public LuaLBuffer(LuaState l)
        {
            StringBuilder = new StringBuilder();
            LuaState = l;
        }
    }
}
