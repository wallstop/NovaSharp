namespace WallstopStudios.NovaSharp.Interpreter.Platforms
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// An abstract class which offers basic services on top of IPlatformAccessor to provide easier implementation of platforms.
    /// </summary>
    public abstract class PlatformAccessorBase : IPlatformAccessor
    {
        /// <summary>
        /// Gets the platform name prefix
        /// </summary>
        /// <returns></returns>
        public abstract string GetPlatformNamePrefix();

        /// <summary>
        /// Gets the name of the platform (used for debug purposes).
        /// </summary>
        /// <returns>
        /// The name of the platform (used for debug purposes)
        /// </returns>
        public string GetPlatformName()
        {
            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.Append(GetPlatformNamePrefix());
            sb.Append('.');

            if (PlatformAutoDetector.IsRunningOnUnity)
            {
                if (PlatformAutoDetector.IsUnityNative)
                {
                    sb.Append("unity.");
                    sb.Append(GetUnityPlatformName());
                    sb.Append('.');
                    sb.Append(GetUnityRuntimeName());
                }
                else
                {
                    if (PlatformAutoDetector.IsRunningOnMono)
                    {
                        sb.Append("unity.dll.mono");
                    }
                    else
                    {
                        sb.Append("unity.dll.unknown");
                    }
                }
            }
            else if (PlatformAutoDetector.IsRunningOnMono)
            {
                sb.Append("mono");
            }
            else
            {
                sb.Append("dotnet");
            }

            if (PlatformAutoDetector.IsPortableFramework)
            {
                sb.Append(".portable");
            }

            if (PlatformAutoDetector.IsRunningOnClr4)
            {
                sb.Append(".clr4");
            }
            else
            {
                sb.Append(".clr2");
            }

#if DOTNET_CORE
            sb.Append(".netcore");
#endif

            if (PlatformAutoDetector.IsRunningOnAot)
            {
                sb.Append(".aot");
            }

            return sb.ToString();
        }

        private static string GetUnityRuntimeName()
        {
#if ENABLE_MONO
            return "mono";
#elif ENABLE_IL2CPP
            return "il2cpp";
#elif ENABLE_DOTNET
            return "dotnet";
#else
            return "unknown";
#endif
        }

        private static string GetUnityPlatformName()
        {
#if UNITY_STANDALONE_OSX
            return "osx";
#elif UNITY_STANDALONE_WIN
            return "win";
#elif UNITY_STANDALONE_LINUX
            return "linux";
#elif UNITY_STANDALONE
            return "standalone";
#elif UNITY_WII
            return "wii";
#elif UNITY_IOS
            return "ios";
#elif UNITY_IPHONE
            return "iphone";
#elif UNITY_ANDROID
            return "android";
#elif UNITY_PS3
            return "ps3";
#elif UNITY_PS4
            return "ps4";
#elif UNITY_SAMSUNGTV
            return "samsungtv";
#elif UNITY_XBOX360
            return "xbox360";
#elif UNITY_XBOXONE
            return "xboxone";
#elif UNITY_TIZEN
            return "tizen";
#elif UNITY_TVOS
            return "tvos";
#elif UNITY_WP_8_1
            return "wp_8_1";
#elif UNITY_WSA_10_0
            return "wsa_10_0";
#elif UNITY_WSA_8_1
            return "wsa_8_1";
#elif UNITY_WSA
            return "wsa";
#elif UNITY_WINRT_10_0
            return "winrt_10_0";
#elif UNITY_WINRT_8_1
            return "winrt_8_1";
#elif UNITY_WINRT
            return "winrt";
#elif UNITY_WEBGL
            return "webgl";
#else
            return "unknownhw";
#endif
        }

        /// <summary>
        /// Default handler for 'print' calls. Can be customized in ScriptOptions
        /// </summary>
        /// <param name="content">The content.</param>
        public abstract void DefaultPrint(string content);

        /// <summary>
        /// DEPRECATED.
        /// This is kept for backward compatibility, see the overload taking a prompt as an input parameter.
        ///
        /// Default handler for interactive line input calls. Can be customized in ScriptOptions.
        /// If an inheriting class wants to give a meaningful implementation, this method MUST be overridden.
        /// </summary>
        /// <returns>null</returns>
        [Obsolete("Replace with DefaultInput(string)")]
        public virtual string DefaultInput()
        {
            return null;
        }

        /// <summary>
        /// Default handler for interactive line input calls. Can be customized in ScriptOptions.
        /// If an inheriting class wants to give a meaningful implementation, this method MUST be overridden.
        /// </summary>
        /// <returns>null</returns>
        public virtual string DefaultInput(string prompt)
        {
            return TryInvokeLegacyDefaultInput();
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
        public abstract Stream OpenFile(
            Script script,
            string filename,
            Encoding encoding,
            string mode
        );

        /// <summary>
        /// Gets a standard stream (stdin, stdout, stderr).
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public abstract Stream GetStandardStream(StandardFileType type);

        /// <summary>
        /// Gets a temporary filename. Used in 'io' and 'os' modules.
        /// Can have an invalid implementation if 'io' and 'os' modules are filtered out.
        /// </summary>
        /// <returns></returns>
        public abstract string GetTempFileName();

        /// <summary>
        /// Exits the process, returning the specified exit code.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="exitCode">The exit code.</param>
        public abstract void ExitFast(int exitCode);

        /// <summary>
        /// Checks if a file exists. Used by the 'os' module.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>
        /// True if the file exists, false otherwise.
        /// </returns>
        public abstract bool FileExists(string file);

        /// <summary>
        /// Deletes the specified file. Used by the 'os' module.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="file">The file.</param>
        public abstract void DeleteFile(string file);

        /// <summary>
        /// Moves the specified file. Used by the 'os' module.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <param name="dst">The DST.</param>
        public abstract void MoveFile(string src, string dst);

        /// <summary>
        /// Executes the specified command line, returning the child process exit code and blocking in the meantime.
        /// Can have an invalid implementation if the 'os' module is filtered out.
        /// </summary>
        /// <param name="cmdline">The cmdline.</param>
        /// <returns></returns>
        public abstract int ExecuteCommand(string cmdline);

        /// <summary>
        /// Filters the CoreModules enumeration to exclude non-supported operations
        /// </summary>
        /// <param name="coreModules">The requested modules.</param>
        /// <returns>
        /// The requested modules, with unsupported modules filtered out.
        /// </returns>
        public abstract CoreModules FilterSupportedCoreModules(CoreModules coreModules);

        /// <summary>
        /// Gets an environment variable. Must be implemented, but an implementation is allowed
        /// to always return null if a more meaningful implementation cannot be achieved or is
        /// not desired.
        /// </summary>
        /// <param name="envvarname">The envvarname.</param>
        /// <returns>
        /// The environment variable value, or null if not found
        /// </returns>
        public abstract string GetEnvironmentVariable(string envvarname);

        /// <summary>
        /// Determines whether the application is running in AOT (ahead-of-time) mode
        /// </summary>
        public virtual bool IsRunningOnAOT()
        {
            return PlatformAutoDetector.IsRunningOnAot;
        }

        private string TryInvokeLegacyDefaultInput()
        {
            MethodInfo legacyOverride = GetType()
                .GetMethod(
                    nameof(DefaultInput),
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null
                );

            if (
                legacyOverride == null
                || legacyOverride.DeclaringType == typeof(PlatformAccessorBase)
            )
            {
                return null;
            }

            object result = legacyOverride.Invoke(this, Array.Empty<object>());
            return result as string;
        }
    }
}
