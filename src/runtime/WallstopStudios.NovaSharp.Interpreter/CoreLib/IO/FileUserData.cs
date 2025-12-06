namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.IO
{
    using System.IO;
    using System.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Abstract class implementing a file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal class FileUserData : StreamFileUserDataBase
    {
        public FileUserData(
            Script script,
            string filename,
            Encoding encoding,
            string mode,
            bool isBinaryMode
        )
        {
            Stream stream = Script.GlobalOptions.Platform.OpenFile(
                script,
                filename,
                encoding,
                mode
            );

            StreamReader reader = (stream.CanRead) ? new StreamReader(stream, encoding) : null;
            StreamWriter writer = (stream.CanWrite) ? new StreamWriter(stream, encoding) : null;

            Initialize(stream, reader, writer, isBinaryMode);
        }
    }
}
