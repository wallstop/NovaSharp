namespace NovaSharp.Interpreter.Modding
{
    using System.IO;

    internal sealed class PhysicalModFileSystem : IModFileSystem
    {
        internal static readonly PhysicalModFileSystem Instance = new();

        private PhysicalModFileSystem() { }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public Stream OpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public string GetFullPath(string path)
        {
            return Path.GetFullPath(path);
        }

        public string GetDirectoryName(string path)
        {
            return Path.GetDirectoryName(path);
        }
    }
}
