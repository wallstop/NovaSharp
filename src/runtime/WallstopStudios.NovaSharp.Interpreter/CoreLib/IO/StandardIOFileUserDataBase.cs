namespace WallstopStudios.NovaSharp.Interpreter.CoreLib.IO
{
    using System.IO;

    /// <summary>
    /// Abstract class implementing an unclosable file Lua userdata. Methods are meant to be called by Lua code.
    /// </summary>
    internal class StandardIoFileUserDataBase : StreamFileUserDataBase
    {
        protected override string Close()
        {
            return ("cannot close standard file");
        }

        /// <summary>
        /// Wraps a standard input stream in a non-closeable Lua userdata instance.
        /// </summary>
        /// <param name="stream">Underlying stream provided by the platform.</param>
        /// <returns>A new userdata wrapper.</returns>
        public static StandardIoFileUserDataBase CreateInputStream(Stream stream)
        {
            StandardIoFileUserDataBase f = new();
            f.Initialize(stream, new StreamReader(stream), null, false);
            return f;
        }

        /// <summary>
        /// Wraps a standard output stream in a non-closeable Lua userdata instance.
        /// </summary>
        /// <param name="stream">Underlying stream provided by the platform.</param>
        /// <returns>A new userdata wrapper.</returns>
        public static StandardIoFileUserDataBase CreateOutputStream(Stream stream)
        {
            StandardIoFileUserDataBase f = new();
            // Standard streams must not hold buffered output past the write call: nothing
            // flushes them at host exit, so buffered bytes would be lost (C leaves stderr
            // unbuffered and the reference standalone interpreter flushes stdout at exit).
            f.Initialize(stream, null, new StreamWriter(stream) { AutoFlush = true }, false);
            return f;
        }
    }
}
