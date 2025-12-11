namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System.IO;

    /// <summary>
    /// Minimal abstraction over the file-system operations needed to read <c>mod.json</c>.
    /// Hosts can inject custom implementations (virtual file systems, Unity assets, etc.).
    /// </summary>
    public interface IModFileSystem
    {
        /// <summary>
        /// Returns <c>true</c> when the specified path points to an existing file.
        /// </summary>
        public bool FileExists(string path);

        /// <summary>
        /// Returns <c>true</c> when the specified path points to an existing directory.
        /// </summary>
        public bool DirectoryExists(string path);

        /// <summary>
        /// Opens the specified file for read-only access.
        /// </summary>
        public Stream OpenRead(string path);

        /// <summary>
        /// Resolves the fully qualified path for the supplied location.
        /// </summary>
        public string GetFullPath(string path);

        /// <summary>
        /// Returns the directory component for the supplied path.
        /// </summary>
        public string GetDirectoryName(string path);
    }
}
