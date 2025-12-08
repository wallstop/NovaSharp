namespace WallstopStudios.NovaSharp.Interpreter.Platforms
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Interop;

    /// <summary>
    /// A static class offering properties for autodetection of system/platform details
    /// </summary>
    public static class PlatformAutoDetector
    {
        private const int RunningOnAotUnknown = -1;
        private const int RunningOnAotFalse = 0;
        private const int RunningOnAotTrue = 1;

        /// <summary>
        /// Caches the result of the JIT detection probe so repeated calls avoid recompiling expressions.
        /// </summary>
        private static int RunningOnAotState = RunningOnAotUnknown;

        /// <summary>
        /// Synchronizes concurrent AOT detection so we only probe once per process.
        /// </summary>
        private static readonly object RunningOnAotStateGate = new();

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
                int cachedState = Volatile.Read(ref RunningOnAotState);
                if (cachedState != RunningOnAotUnknown)
                {
                    return cachedState == RunningOnAotTrue;
                }

                lock (RunningOnAotStateGate)
                {
                    cachedState = Volatile.Read(ref RunningOnAotState);
                    if (cachedState == RunningOnAotUnknown)
                    {
                        bool result = ProbeIsRunningOnAot();
                        Volatile.Write(
                            ref RunningOnAotState,
                            result ? RunningOnAotTrue : RunningOnAotFalse
                        );
                        return result;
                    }

                    return cachedState == RunningOnAotTrue;
                }
#endif
            }
        }

#if !(UNITY_WEBGL || UNITY_IOS || UNITY_TVOS || ENABLE_IL2CPP)
        private static bool ProbeIsRunningOnAot()
        {
            try
            {
                Func<bool> probeOverride = TestHooks.GetAotProbeOverride();
                if (probeOverride != null)
                {
                    return probeOverride();
                }

                Expression e = Expression.Constant(5, typeof(int));
                Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(e);
                lambda.Compile();
                return false;
            }
            catch (Exception ex) when (IsAotDetectionSuppressedException(ex))
            {
                return true;
            }
        }
#endif

        private static void AutoDetectPlatformFlags()
        {
            if (AutoDetectionsDone)
            {
                return;
            }

            try
            {
                bool? forcedUnity = TestHooks.GetUnityDetectionOverride();

                if (forcedUnity.HasValue)
                {
                    IsRunningOnUnity = forcedUnity.Value;
                    if (!forcedUnity.Value)
                    {
                        IsUnityNative = false;
                        IsUnityIl2Cpp = false;
                    }
                }
                else
                {
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
                    Assembly[] assemblyOverride = TestHooks
                        .GetAssemblyEnumerationOverride()
                        ?.Invoke();
                    Assembly[] loadedAssemblies =
                        assemblyOverride?.Where(a => a != null).ToArray()
                        ?? AppDomain.CurrentDomain.GetAssemblies();
                    bool unityTypeFound = false;

                    for (
                        int asmIndex = 0;
                        asmIndex < loadedAssemblies.Length && !unityTypeFound;
                        asmIndex++
                    )
                    {
                        Assembly assembly = loadedAssemblies[asmIndex];

                        string assemblyName = assembly?.GetName().Name;
                        if (
                            assemblyName != null
                            && assemblyName.StartsWith("UnityEngine.", StringComparison.Ordinal)
                        )
                        {
                            unityTypeFound = true;
                            break;
                        }

                        Type[] assemblyTypes = assembly.SafeGetTypes();
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
                }
            }
            finally
            {
                IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);
                IsRunningOnClr4 = (Type.GetType("System.Lazy`1") != null);
                AutoDetectionsDone = true;
            }
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
                bool autoDetectionsDone,
                Func<bool> aotProbeOverride,
                bool? unityDetectionOverride
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
                AotProbeOverride = aotProbeOverride;
                UnityDetectionOverride = unityDetectionOverride;
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

            /// <summary>Gets the captured AOT probe override delegate.</summary>
            internal Func<bool> AotProbeOverride { get; }

            /// <summary>Gets the captured Unity detection override flag.</summary>
            internal bool? UnityDetectionOverride { get; }
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
                    ConvertStateToNullable(Volatile.Read(ref RunningOnAotState)),
                    AutoDetectionsDone,
                    AotProbeOverride,
                    UnityDetectionOverride
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
                SetRunningOnAot(snapshot.RunningOnAotCache);
                AutoDetectionsDone = snapshot.AutoDetectionsDone;
                AotProbeOverride = snapshot.AotProbeOverride;
                UnityDetectionOverride = snapshot.UnityDetectionOverride;
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
                Volatile.Write(ref RunningOnAotState, ConvertNullableToState(value));
            }

            /// <summary>
            /// Overrides whether auto-detection has already run.
            /// </summary>
            public static void SetAutoDetectionsDone(bool value)
            {
                AutoDetectionsDone = value;
            }

            /// <summary>
            /// Overrides the AOT probe logic; when set, <see cref="IsRunningOnAot"/> uses the supplied delegate.
            /// </summary>
            /// <remarks>
            /// When setting a non-null probe override, this method acquires <see cref="RunningOnAotStateGate"/>
            /// to atomically set the override and invalidate any in-flight probes that may have started
            /// without the override. When clearing the override (null), the cached state is preserved
            /// to allow tests to verify caching behavior.
            /// </remarks>
            public static void SetAotProbeOverride(Func<bool> probe)
            {
                if (probe != null)
                {
                    lock (RunningOnAotStateGate)
                    {
                        AotProbeOverride = probe;
                        Volatile.Write(ref RunningOnAotState, RunningOnAotUnknown);
                    }
                }
                else
                {
                    AotProbeOverride = null;
                }
            }

            /// <summary>
            /// Returns the delegate currently overriding the default AOT probe, if any.
            /// </summary>
            internal static Func<bool> GetAotProbeOverride()
            {
                return AotProbeOverride;
            }

            /// <summary>
            /// Overrides Unity detection results so tests can force Unity/non-Unity behaviour.
            /// </summary>
            /// <param name="value">Forced Unity flag; <c>null</c> removes the override.</param>
            public static void SetUnityDetectionOverride(bool? value)
            {
                UnityDetectionOverride = value;
            }

            /// <summary>
            /// Gets the currently applied Unity detection override, if any.
            /// </summary>
            internal static bool? GetUnityDetectionOverride()
            {
                return UnityDetectionOverride;
            }

            /// <summary>
            /// Must be volatile to ensure visibility across threads when probe overrides are
            /// set for testing. The delegate is read inside the <see cref="RunningOnAotStateGate"/>
            /// lock by <see cref="ProbeIsRunningOnAot"/>, but written atomically with the cache
            /// reset by <see cref="SetAotProbeOverride"/>. Using volatile ensures the read
            /// inside the lock sees the latest write.
            /// </summary>
            private static volatile Func<bool> AotProbeOverride;

            private static bool? UnityDetectionOverride;

            private static Func<Assembly[]> AssemblyEnumerationOverride;

            /// <summary>
            /// Overrides the assembly enumeration used when probing for Unity types.
            /// </summary>
            public static void SetAssemblyEnumerationOverride(Func<Assembly[]> provider)
            {
                AssemblyEnumerationOverride = provider;
            }

            /// <summary>
            /// Gets the currently configured assembly enumeration override, if any.
            /// </summary>
            internal static Func<Assembly[]> GetAssemblyEnumerationOverride()
            {
                return AssemblyEnumerationOverride;
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

        private static bool? ConvertStateToNullable(int state)
        {
            return state switch
            {
                RunningOnAotTrue => true,
                RunningOnAotFalse => false,
                _ => null,
            };
        }

        private static int ConvertNullableToState(bool? value)
        {
            if (!value.HasValue)
            {
                return RunningOnAotUnknown;
            }

            return value.Value ? RunningOnAotTrue : RunningOnAotFalse;
        }
    }
}
