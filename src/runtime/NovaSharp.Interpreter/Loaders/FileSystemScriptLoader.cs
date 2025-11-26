namespace NovaSharp.Interpreter.Loaders
{
#if DOTNET_CORE || (!(PCL || ENABLE_DOTNET || NETFX_CORE))
    using System;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Utilities;
    using System.IO;

    /// <summary>
    /// A script loader loading scripts directly from the file system (does not go through platform object)
    /// </summary>
    public class FileSystemScriptLoader : ScriptLoaderBase
    {
        /// <summary>
        /// Checks if a script file exists.
        /// </summary>
        /// <param name="name">The script filename.</param>
        /// <returns></returns>
        public override bool ScriptFileExists(string name) =>
            File.Exists(NormalizePath(name, nameof(name)));

        /// <summary>
        /// Opens a file for reading the script code.
        /// It can return either a string, a byte[] or a Stream.
        /// If a byte[] is returned, the content is assumed to be a serialized (dumped) bytecode. If it's a string, it's
        /// assumed to be either a script or the output of a string.dump call. If a Stream, autodetection takes place.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>
        /// A string, a byte[] or a Stream.
        /// </returns>
        public override object LoadFile(string file, Table globalContext) =>
            new FileStream(
                NormalizePath(file, nameof(file)),
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

        private static string NormalizePath(string path, string paramName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(paramName);
            }

            ReadOnlySpan<char> trimmed = path.AsSpan().TrimWhitespace();
            string candidate = trimmed.Length == path.Length ? path : new string(trimmed);
            return candidate.NormalizeDirectorySeparators(Path.DirectorySeparatorChar);
        }
    }
}
#endif
