namespace WallstopStudios.NovaSharp.Interpreter.Platforms
{
#if DOTNET_CORE
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Class providing the IPlatformAccessor interface for .NET Core builds
    /// </summary>
    public class DotNetCorePlatformAccessor : PlatformAccessorBase
    {
        /// <summary>
        /// Converts a Lua string access mode to a FileAccess enum
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static FileAccess ParseFileAccess(string mode)
        {
            string normalized = NormalizeMode(mode);

            return normalized switch
            {
                "r" => FileAccess.Read,
                "r+" => FileAccess.ReadWrite,
                "w" => FileAccess.Write,
                "w+" => FileAccess.ReadWrite,
                _ => FileAccess.ReadWrite,
            };
        }

        /// <summary>
        /// Converts a Lua string access mode to a ParseFileMode enum
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <returns></returns>
        public static FileMode ParseFileMode(string mode)
        {
            string normalized = NormalizeMode(mode);

            return normalized switch
            {
                "r" => FileMode.Open,
                "r+" => FileMode.OpenOrCreate,
                "w" => FileMode.Create,
                "w+" => FileMode.Truncate,
                _ => FileMode.Append,
            };
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
                    throw new InvalidEnumArgumentException(
                        nameof(type),
                        (int)type,
                        typeof(StandardFileType)
                    );
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
            if (string.IsNullOrWhiteSpace(cmdline))
            {
                return 0;
            }

            ProcessStartInfo startInfo = BuildShellProcessStartInfo(cmdline);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            try
            {
                using Process process =
                    Process.Start(startInfo)
                    ?? throw new InvalidOperationException("Failed to start command process.");
                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Filters the CoreModules enumeration to exclude non-supported operations
        /// </summary>
        /// <param name="coreModules">The requested modules.</param>
        /// <returns>
        /// The requested modules, with unsupported modules filtered out.
        /// </returns>
        public override CoreModules FilterSupportedCoreModules(CoreModules coreModules)
        {
            return coreModules;
        }

        /// <summary>
        /// Gets the platform name prefix
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string GetPlatformNamePrefix()
        {
            return "core";
        }

        private static ProcessStartInfo BuildShellProcessStartInfo(string cmdline)
        {
            ProcessStartInfo startInfo = new();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.FileName = "cmd.exe";
                startInfo.ArgumentList.Add("/C");
                startInfo.ArgumentList.Add(cmdline);
            }
            else
            {
                startInfo.FileName = "/bin/sh";
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add(cmdline);
            }

            return startInfo;
        }

        private static string NormalizeMode(string mode)
        {
            if (mode == null)
            {
                throw new ArgumentNullException(nameof(mode));
            }

            string trimmed = mode.Trim();

            if (trimmed.Length == 0)
            {
                return string.Empty;
            }

            char[] buffer = new char[trimmed.Length];
            int index = 0;

            foreach (char c in trimmed)
            {
                char lowered = char.ToLowerInvariant(c);
                if (char.IsWhiteSpace(lowered))
                {
                    continue;
                }

                if (lowered == 'b')
                {
                    continue;
                }

                buffer[index++] = lowered;
            }

            return new string(buffer, 0, index);
        }
    }
#endif
}
