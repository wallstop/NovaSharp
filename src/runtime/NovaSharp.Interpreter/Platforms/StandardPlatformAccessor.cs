namespace NovaSharp.Interpreter.Platforms
{
#if (PCL) || (UNITY_5) || NETFX_CORE
    // Dummy implementation for PCL and Unity targets
    using NovaSharp.Interpreter.Modules;
    using System;
    using System.IO;
    using System.Text;

    namespace NovaSharp.Interpreter.Platforms
    {
        /// <summary>
        /// Class providing the IPlatformAccessor interface for standard full-feaured implementations.
        /// </summary>
        public class StandardPlatformAccessor : PlatformAccessorBase
        {
            /// <inheritdoc/>
            public override void DefaultPrint(string content)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override CoreModules FilterSupportedCoreModules(CoreModules module)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override string GetEnvironmentVariable(string envvarname)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override string GetPlatformNamePrefix()
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override Stream GetStandardStream(StandardFileType type)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override Stream OpenFile(
                Script script,
                string filename,
                Encoding encoding,
                string mode
            )
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override string GetTempFileName()
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override int ExecuteCommand(string cmdline)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override void ExitFast(int exitCode)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override void DeleteFile(string file)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override bool FileExists(string file)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public override void MoveFile(string src, string dst)
            {
                throw new NotImplementedException();
            }
        }
    }
#else
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class providing the IPlatformAccessor interface for standard full-feaured implementations.
    /// </summary>
    public class StandardPlatformAccessor : PlatformAccessorBase
    {
        /// <summary>
        /// Converts a Lua string access mode to a FileAccess enum
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static FileAccess ParseFileAccess(string mode)
        {
            string normalizedMode = NormalizeMode(mode);

            if (normalizedMode == "r")
            {
                return FileAccess.Read;
            }
            else if (normalizedMode == "r+")
            {
                return FileAccess.ReadWrite;
            }
            else if (normalizedMode == "w")
            {
                return FileAccess.Write;
            }
            else if (normalizedMode == "w+")
            {
                return FileAccess.ReadWrite;
            }
            else
            {
                return FileAccess.ReadWrite;
            }
        }

        /// <summary>
        /// Converts a Lua string access mode to a ParseFileMode enum
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static FileMode ParseFileMode(string mode)
        {
            string normalizedMode = NormalizeMode(mode);

            if (normalizedMode == "r")
            {
                return FileMode.Open;
            }
            else if (normalizedMode == "r+")
            {
                return FileMode.OpenOrCreate;
            }
            else if (normalizedMode == "w")
            {
                return FileMode.Create;
            }
            else if (normalizedMode == "w+")
            {
                return FileMode.Truncate;
            }
            else
            {
                return FileMode.Append;
            }
        }

        /// <summary>
        /// A function used to open files in the 'io' module.
        /// Can have an invalid implementation if 'io' module is filtered out.
        /// It should return a correctly initialized Stream for the given file and access
        /// </summary>
        /// <param name="script"></param>
        /// <param name="filename">The filename.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="mode">The mode (as per Lua usage - e.g. 'w+', 'rb', etc.).</param>
        /// <returns></returns>
        public override Stream OpenFile(
            Script script,
            string filename,
            Encoding encoding,
            string mode
        )
        {
            return new FileStream(
                filename,
                ParseFileMode(mode),
                ParseFileAccess(mode),
                FileShare.ReadWrite | FileShare.Delete
            );
        }

        /// <summary>
        /// Gets an environment variable. Must be implemented, but an implementation is allowed
        /// to always return null if a more meaningful implementation cannot be achieved or is
        /// not desired.
        /// </summary>
        /// <param name="envvarname">The envvarname.</param>
        /// <returns>
        /// The environment variable value, or null if not found
        /// </returns>
        public override string GetEnvironmentVariable(string envvarname)
        {
            return Environment.GetEnvironmentVariable(envvarname);
        }

        /// <summary>
        /// Gets a standard stream (stdin, stdout, stderr).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">type</exception>
        public override Stream GetStandardStream(StandardFileType type)
        {
            switch (type)
            {
                case StandardFileType.StdIn:
                    return Console.OpenStandardInput();
                case StandardFileType.StdOut:
                    return Console.OpenStandardOutput();
                case StandardFileType.StdErr:
                    return Console.OpenStandardError();
                default:
                    throw new ArgumentException("Unknown standard file type.", nameof(type));
            }
        }

        /// <summary>
        /// Default handler for 'print' calls. Can be customized in ScriptOptions
        /// </summary>
        /// <param name="content">The content.</param>
        public override void DefaultPrint(string content)
        {
            Console.WriteLine(content);
        }

        /// <summary>
        /// Gets a temporary filename. Used in 'io' and 'os' modules.
        /// Can have an invalid implementation if 'io' and 'os' modules are filtered out.
        /// </summary>
        /// <returns></returns>
        public override string GetTempFileName()
        {
            return Path.GetTempFileName();
        }

        /// <summary>
        /// Exits the process, returning the specified exit code.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        public override void ExitFast(int exitCode)
        {
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Checks if a file exists. Used by the 'os' module.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        /// True if the file exists, false otherwise.
        /// </returns>
        public override bool FileExists(string file)
        {
            return File.Exists(file);
        }

        /// <summary>
        /// Deletes the specified file. Used by the 'os' module.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="file">The file.</param>
        public override void DeleteFile(string file)
        {
            File.Delete(file);
        }

        /// <summary>
        /// Moves the specified file. Used by the 'os' module.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="dst">The DST.</param>
        public override void MoveFile(string src, string dst)
        {
#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE)
            File.Move(src, dst);
#endif
        }

        /// <summary>
        /// Executes the specified command line, returning the child process exit code and blocking in the meantime.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="cmdline">The cmdline.</param>
        /// <returns></returns>
        public override int ExecuteCommand(string cmdline)
        {
            // This is windows only!
            ProcessStartInfo psi = new("cmd.exe", $"/C {cmdline}") { ErrorDialog = false };

            Process proc = Process.Start(psi);
            proc.WaitForExit();
            return proc.ExitCode;
        }

        /// <summary>
        /// Filters the CoreModules enumeration to exclude non-supported operations
        /// </summary>
        /// <param name="module">The requested modules.</param>
        /// <returns>
        /// The requested modules, with unsupported modules filtered out.
        /// </returns>
        public override CoreModules FilterSupportedCoreModules(CoreModules module)
        {
            return module;
        }

        /// <summary>
        /// Gets the platform name prefix
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string GetPlatformNamePrefix()
        {
            return "std";
        }

        private static string NormalizeMode(string mode)
        {
            if (mode == null)
            {
                throw new ArgumentNullException(nameof(mode));
            }

            if (mode.AsSpan().IndexOf("b", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return mode;
            }

            char[] buffer = new char[mode.Length];
            int bufferIndex = 0;

            foreach (char c in mode)
            {
                if (c != 'b' && c != 'B')
                {
                    buffer[bufferIndex++] = c;
                }
            }

            return new string(buffer, 0, bufferIndex);
        }
    }
#endif
}
