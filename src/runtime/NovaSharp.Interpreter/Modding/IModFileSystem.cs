namespace NovaSharp.Interpreter.Modding
{
    using System.IO;

    /// <summary>
    /// Minimal abstraction over the file-system operations needed to read <c>mod.json</c>.
    /// Hosts can inject custom implementations (virtual file systems, Unity assets, etc.).
    /// </summary>
    public interface IModFileSystem
    {
        public bool FileExists(string path);

        public bool DirectoryExists(string path);

        public Stream OpenRead(string path);

        public string GetFullPath(string path);

        public string GetDirectoryName(string path);
    }
}
