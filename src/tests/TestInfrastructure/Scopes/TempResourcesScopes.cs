namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Provides a temporary directory that is automatically deleted when disposed.
    /// </summary>
    internal sealed class TempDirectoryScope : IDisposable
    {
        private readonly bool _deleteOnDispose;

        private TempDirectoryScope(string directoryPath, bool ensureCreated, bool deleteOnDispose)
        {
            if (string.IsNullOrEmpty(directoryPath))
            {
                throw new ArgumentException(
                    "Directory path must be provided.",
                    nameof(directoryPath)
                );
            }

            DirectoryPath = directoryPath;
            _deleteOnDispose = deleteOnDispose;

            if (ensureCreated)
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public string DirectoryPath { get; }

        public static TempDirectoryScope Create(
            string parentDirectory = null,
            string namePrefix = "NovaSharpTests_",
            bool deleteOnDispose = true
        )
        {
            string baseDirectory = string.IsNullOrEmpty(parentDirectory)
                ? Path.GetTempPath()
                : parentDirectory;
            string prefix = string.IsNullOrEmpty(namePrefix) ? "NovaSharpTests_" : namePrefix;
            string directoryName = prefix + Guid.NewGuid().ToString("N");
            string directoryPath = Path.Combine(baseDirectory, directoryName);

            return new TempDirectoryScope(directoryPath, ensureCreated: true, deleteOnDispose);
        }

        public static TempDirectoryScope FromExisting(
            string directoryPath,
            bool deleteOnDispose = true
        )
        {
            return new TempDirectoryScope(directoryPath, ensureCreated: false, deleteOnDispose);
        }

        public void Dispose()
        {
            if (!_deleteOnDispose)
            {
                return;
            }

            try
            {
                if (Directory.Exists(DirectoryPath))
                {
                    Directory.Delete(DirectoryPath, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best effort cleanup for tests; ignore IO races.
            }
            catch (UnauthorizedAccessException)
            {
                // Some platforms lock files temporarily; ignore so tests can complete.
            }
        }
    }

    /// <summary>
    /// Provides a temporary file that is deleted on disposal.
    /// </summary>
    internal sealed class TempFileScope : IDisposable
    {
        private readonly bool _deleteOnDispose;

        private TempFileScope(string filePath, bool createFile, bool deleteOnDispose)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path must be provided.", nameof(filePath));
            }

            FilePath = filePath;
            _deleteOnDispose = deleteOnDispose;

            if (createFile && !File.Exists(filePath))
            {
                string directoryName = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                using (FileStream stream = File.Create(filePath))
                {
                    // Ensure the file exists for the caller.
                }
            }
        }

        public string FilePath { get; }

        public string EscapedPath => FilePath.Replace("\\", "\\\\", StringComparison.Ordinal);

        public static TempFileScope Create(
            string directory = null,
            string namePrefix = "NovaSharpTests_",
            string extension = ".tmp",
            bool createFile = false,
            bool deleteOnDispose = true
        )
        {
            string baseDirectory = string.IsNullOrEmpty(directory) ? Path.GetTempPath() : directory;
            string prefix = string.IsNullOrEmpty(namePrefix) ? "NovaSharpTests_" : namePrefix;
            string fileExtension = NormalizeExtension(extension);
            string fileName = prefix + Guid.NewGuid().ToString("N") + fileExtension;
            string filePath = Path.Combine(baseDirectory, fileName);

            return new TempFileScope(filePath, createFile, deleteOnDispose);
        }

        public static TempFileScope CreateEmpty(
            string directory = null,
            string namePrefix = "NovaSharpTests_",
            string extension = ".txt",
            bool deleteOnDispose = true
        )
        {
            return CreateWithText(
                string.Empty,
                encoding: null,
                directory,
                namePrefix,
                extension,
                deleteOnDispose
            );
        }

        public static TempFileScope CreateWithText(
            string content,
            Encoding encoding = null,
            string directory = null,
            string namePrefix = "NovaSharpTests_",
            string extension = ".txt",
            bool deleteOnDispose = true
        )
        {
            TempFileScope scope = Create(
                directory,
                namePrefix,
                extension,
                createFile: false,
                deleteOnDispose: deleteOnDispose
            );
            Encoding writerEncoding =
                encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(scope.FilePath, content ?? string.Empty, writerEncoding);
            return scope;
        }

        public static TempFileScope CreateWithBytes(
            byte[] data,
            string directory = null,
            string namePrefix = "NovaSharpTests_",
            string extension = ".bin",
            bool deleteOnDispose = true
        )
        {
            TempFileScope scope = Create(
                directory,
                namePrefix,
                extension,
                createFile: false,
                deleteOnDispose: deleteOnDispose
            );
            File.WriteAllBytes(scope.FilePath, data ?? Array.Empty<byte>());
            return scope;
        }

        public static TempFileScope FromExisting(string filePath, bool deleteOnDispose = true)
        {
            return new TempFileScope(filePath, createFile: false, deleteOnDispose);
        }

        public void Dispose()
        {
            if (!_deleteOnDispose)
            {
                return;
            }

            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (IOException)
            {
                // Tests should not fail when cleanup races with other IO.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore handles that are still draining.
            }
        }

        private static string NormalizeExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return ".tmp";
            }

            if (extension.Length > 0 && extension[0] == '.')
            {
                return extension;
            }

            return "." + extension;
        }
    }
}
