namespace NovaSharp.Interpreter.CoreLib.IO
{
    using System.IO;
    using NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Abstract class implementing an unclosable file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal class StandardIoFileUserDataBase : StreamFileUserDataBase
    {
        protected override string Close()
        {
            return ("cannot close standard file");
        }

        public static StandardIoFileUserDataBase CreateInputStream(Stream stream)
        {
            StandardIoFileUserDataBase f = new();
            f.Initialize(stream, new StreamReader(stream), null);
            return f;
        }

        public static StandardIoFileUserDataBase CreateOutputStream(Stream stream)
        {
            StandardIoFileUserDataBase f = new();
            f.Initialize(stream, null, new StreamWriter(stream));
            return f;
        }
    }
}
