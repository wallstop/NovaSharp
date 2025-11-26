namespace NovaSharp.Interpreter.Modding
{
    using System.IO;

    /// <summary>
    /// Default <see cref="IModFileSystem"/> implementation that forwards directly to <see cref="System.IO"/> APIs.
    /// </summary>
    internal sealed class PhysicalModFileSystem : IModFileSystem
    {
        /// <summary>
        /// Shared instance consumed by helpers when hosts do not inject their own filesystem.
        /// </summary>
        internal static readonly PhysicalModFileSystem Instance = new();

        private PhysicalModFileSystem() { }

        /// <inheritdoc/>
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        /// <inheritdoc/>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <inheritdoc/>
        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        /// <inheritdoc/>
        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        /// <inheritdoc/>
        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }
    }
}
