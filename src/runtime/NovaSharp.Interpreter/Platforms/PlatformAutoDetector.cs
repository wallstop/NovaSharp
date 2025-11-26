namespace NovaSharp.Interpreter.Platforms
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using Loaders;
    using NovaSharp.Interpreter.Interop;

    /// <summary>
    /// A static class offering properties for autodetection of system/platform details
    /// </summary>
    public static class PlatformAutoDetector
    {
        /// <summary>
        /// Caches the result of the JIT detection probe so repeated calls avoid recompiling expressions.
        /// </summary>
        private static bool? RunningOnAotCache;

        /// <summary>
        /// Tracks whether the expensive detection logic already populated the platform flags.
        /// </summary>
        private static bool AutoDetectionsDone;

        /// <summary>
        /// Gets a value indicating whether this instance is running on mono.
        /// </summary>
        public static bool IsRunningOnMono { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running on a CLR4 compatible implementation
        /// </summary>
        public static bool IsRunningOnClr4 { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running on Unity-3D
        /// </summary>
        public static bool IsRunningOnUnity { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has been built as a Portable Class Library
        /// </summary>
        public static bool IsPortableFramework { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has been compiled natively in Unity (as opposite to importing a DLL).
        /// </summary>
        public static bool IsUnityNative { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has been compiled natively in Unity AND is using IL2CPP
        /// </summary>
        public static bool IsUnityIl2Cpp { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running a system using Ahead-Of-Time compilation
        /// and not supporting JIT.
        /// </summary>
        public static bool IsRunningOnAot
        {
            // We do a lazy eval here, so we can wire out this code by not calling it, if necessary..
            get
            {
#if UNITY_WEBGL || UNITY_IOS || UNITY_TVOS || ENABLE_IL2CPP
                return true;
#else

                if (!RunningOnAotCache.HasValue)
                {
                    try
                    {
                        Expression e = Expression.Constant(5, typeof(int));
                        Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(e);
                        lambda.Compile();
                        RunningOnAotCache = false;
                    }
                    catch (Exception ex) when (IsAotDetectionSuppressedException(ex))
                    {
                        RunningOnAotCache = true;
                    }
                }

                return RunningOnAotCache.Value;
#endif
            }
        }

        private static void AutoDetectPlatformFlags()
        {
            if (AutoDetectionsDone)
            {
                return;
            }
#if PCL
            IsPortableFramework = true;
#if ENABLE_DOTNET
            IsRunningOnUnity = true;
            IsUnityNative = true;
#endif
#else
#if UNITY_5
            IsRunningOnUnity = true;
            IsUnityNative = true;

#if ENABLE_IL2CPP
            IsUnityIL2CPP = true;
#endif
#elif !(NETFX_CORE)
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool unityTypeFound = false;

            for (
                int asmIndex = 0;
                asmIndex < loadedAssemblies.Length && !unityTypeFound;
                asmIndex++
            )
            {
                Type[] assemblyTypes = loadedAssemblies[asmIndex].SafeGetTypes();
                for (int typeIndex = 0; typeIndex < assemblyTypes.Length; typeIndex++)
                {
                    if (
                        assemblyTypes[typeIndex]
                            .FullName.StartsWith("UnityEngine.", StringComparison.Ordinal)
                    )
                    {
                        unityTypeFound = true;
                        break;
                    }
                }
            }

            IsRunningOnUnity = unityTypeFound;
#endif
#endif

            IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);

            IsRunningOnClr4 = (Type.GetType("System.Lazy`1") != null);

            AutoDetectionsDone = true;
        }

        /// <summary>
        /// Returns the default platform accessor for the current runtime.
        /// </summary>
        internal static IPlatformAccessor GetDefaultPlatform()
        {
            AutoDetectPlatformFlags();

#if PCL || ENABLE_DOTNET
            return new LimitedPlatformAccessor();
#else
            if (IsRunningOnUnity)
            {
                return new LimitedPlatformAccessor();
            }

#if DOTNET_CORE
            return new DotNetCorePlatformAccessor();
#else
            return new StandardPlatformAccessor();
#endif
#endif
        }

        /// <summary>
        /// Returns the default script loader for the current runtime.
        /// </summary>
        internal static IScriptLoader GetDefaultScriptLoader()
        {
            AutoDetectPlatformFlags();

            if (IsRunningOnUnity)
            {
                return new UnityAssetsScriptLoader();
            }
            else
            {
#if (DOTNET_CORE)
                return new FileSystemScriptLoader();
#elif (PCL || ENABLE_DOTNET || NETFX_CORE)
                return new InvalidScriptLoader("Portable Framework");
#else
                return new FileSystemScriptLoader();
#endif
            }
        }

        /// <summary>
        /// Captures the platform detection state so tests can restore it later.
        /// </summary>
        internal sealed class PlatformDetectorSnapshot
        {
            internal PlatformDetectorSnapshot(
                bool isRunningOnMono,
                bool isRunningOnClr4,
                bool isRunningOnUnity,
                bool isPortableFramework,
                bool isUnityNative,
                bool isUnityIl2Cpp,
                bool? runningOnAotCache,
                bool autoDetectionsDone
            )
            {
                IsRunningOnMono = isRunningOnMono;
                IsRunningOnClr4 = isRunningOnClr4;
                IsRunningOnUnity = isRunningOnUnity;
                IsPortableFramework = isPortableFramework;
                IsUnityNative = isUnityNative;
                IsUnityIl2Cpp = isUnityIl2Cpp;
                RunningOnAotCache = runningOnAotCache;
                AutoDetectionsDone = autoDetectionsDone;
            }

            /// <summary>Gets the captured Mono detection flag.</summary>
            internal bool IsRunningOnMono { get; }

            /// <summary>Gets the captured CLR4 detection flag.</summary>
            internal bool IsRunningOnClr4 { get; }

            /// <summary>Gets the captured Unity detection flag.</summary>
            internal bool IsRunningOnUnity { get; }

            /// <summary>Gets the captured portable-framework detection flag.</summary>
            internal bool IsPortableFramework { get; }

            /// <summary>Gets the captured Unity-native detection flag.</summary>
            internal bool IsUnityNative { get; }

            /// <summary>Gets the captured IL2CPP detection flag.</summary>
            internal bool IsUnityIl2Cpp { get; }

            /// <summary>Gets the cached AOT probe result included in the snapshot.</summary>
            internal bool? RunningOnAotCache { get; }

            /// <summary>Gets a value indicating whether auto-detection had already run.</summary>
            internal bool AutoDetectionsDone { get; }
        }

        /// <summary>
        /// Provides test-only hooks for capturing/restoring detector state.
        /// </summary>
        internal static class TestHooks
        {
            /// <summary>
            /// Takes a snapshot of the current detector state.
            /// </summary>
            public static PlatformDetectorSnapshot CaptureState()
            {
                return new PlatformDetectorSnapshot(
                    IsRunningOnMono,
                    IsRunningOnClr4,
                    IsRunningOnUnity,
                    IsPortableFramework,
                    IsUnityNative,
                    IsUnityIl2Cpp,
                    RunningOnAotCache,
                    AutoDetectionsDone
                );
            }

            /// <summary>
            /// Restores the detector to a previously captured state.
            /// </summary>
            public static void RestoreState(PlatformDetectorSnapshot snapshot)
            {
                IsRunningOnMono = snapshot.IsRunningOnMono;
                IsRunningOnClr4 = snapshot.IsRunningOnClr4;
                IsRunningOnUnity = snapshot.IsRunningOnUnity;
                IsPortableFramework = snapshot.IsPortableFramework;
                IsUnityNative = snapshot.IsUnityNative;
                IsUnityIl2Cpp = snapshot.IsUnityIl2Cpp;
                RunningOnAotCache = snapshot.RunningOnAotCache;
                AutoDetectionsDone = snapshot.AutoDetectionsDone;
            }

            /// <summary>
            /// Overrides individual detection flags for test scenarios.
            /// </summary>
            public static void SetFlags(
                bool? isRunningOnMono = null,
                bool? isRunningOnClr4 = null,
                bool? isRunningOnUnity = null,
                bool? isPortableFramework = null,
                bool? isUnityNative = null,
                bool? isUnityIl2Cpp = null
            )
            {
                if (isRunningOnMono.HasValue)
                {
                    IsRunningOnMono = isRunningOnMono.Value;
                }

                if (isRunningOnClr4.HasValue)
                {
                    IsRunningOnClr4 = isRunningOnClr4.Value;
                }

                if (isRunningOnUnity.HasValue)
                {
                    IsRunningOnUnity = isRunningOnUnity.Value;
                }

                if (isPortableFramework.HasValue)
                {
                    IsPortableFramework = isPortableFramework.Value;
                }

                if (isUnityNative.HasValue)
                {
                    IsUnityNative = isUnityNative.Value;
                }

                if (isUnityIl2Cpp.HasValue)
                {
                    IsUnityIl2Cpp = isUnityIl2Cpp.Value;
                }
            }

            /// <summary>
            /// Overrides the cached AOT detection result.
            /// </summary>
            public static void SetRunningOnAot(bool? value)
            {
                RunningOnAotCache = value;
            }

            /// <summary>
            /// Overrides whether auto-detection has already run.
            /// </summary>
            public static void SetAutoDetectionsDone(bool value)
            {
                AutoDetectionsDone = value;
            }
        }

        private static bool IsAotDetectionSuppressedException(Exception ex)
        {
            return ex is PlatformNotSupportedException
                || ex is MemberAccessException
                || ex is NotSupportedException
                || ex is InvalidOperationException
                || ex is TypeLoadException
                || ex is System.Security.SecurityException;
        }
    }
}
