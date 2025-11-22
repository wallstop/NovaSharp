namespace NovaSharp.Interpreter.Platforms
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Loaders;
    using NovaSharp.Interpreter.Interop;

    /// <summary>
    /// A static class offering properties for autodetection of system/platform details
    /// </summary>
    public static class PlatformAutoDetector
    {
        private static bool? RunningOnAotCache;

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
                    catch (Exception)
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
            IsRunningOnUnity = AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(a => a.SafeGetTypes())
                .Any(t => t.FullName.StartsWith("UnityEngine."));
#endif
#endif

            IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);

            IsRunningOnClr4 = (Type.GetType("System.Lazy`1") != null);

            AutoDetectionsDone = true;
        }

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

            internal bool IsRunningOnMono { get; }
            internal bool IsRunningOnClr4 { get; }
            internal bool IsRunningOnUnity { get; }
            internal bool IsPortableFramework { get; }
            internal bool IsUnityNative { get; }
            internal bool IsUnityIl2Cpp { get; }
            internal bool? RunningOnAotCache { get; }
            internal bool AutoDetectionsDone { get; }
        }

        internal static class TestHooks
        {
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

            public static void SetRunningOnAot(bool? value)
            {
                RunningOnAotCache = value;
            }

            public static void SetAutoDetectionsDone(bool value)
            {
                AutoDetectionsDone = value;
            }
        }
    }
}
