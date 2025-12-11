namespace WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using System.Linq;
    using System.Reflection;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;

    /// <summary>
    /// Provides shared helpers for capturing and mutating platform detector state in tests.
    /// </summary>
    internal sealed class PlatformDetectorScope : IDisposable
    {
        private readonly PlatformDetectorOverrideScope _overrideScope;

        private PlatformDetectorScope(Action initializer)
        {
            _overrideScope = PlatformDetectorOverrideScope.Apply(initializer);
        }

        private static void ApplyResetDefaults()
        {
            PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(null);
            PlatformAutoDetector.TestHooks.SetFlags(
                isRunningOnUnity: false,
                isUnityNative: false,
                isRunningOnMono: false,
                isPortableFramework: false,
                isRunningOnClr4: false,
                isUnityIl2Cpp: false
            );
            SetAotValue(null);
            SetAutoDetectionsDone(false);
            ClearAssemblyEnumerationOverride();
            PlatformAutoDetector.TestHooks.SetAotProbeOverride(null);
        }

        /// <summary>
        /// Captures the current detector state without mutating it.
        /// </summary>
        public static PlatformDetectorScope CaptureStateOnly()
        {
            return new PlatformDetectorScope(static () => { });
        }

        /// <summary>
        /// Resets the detector so tests can exercise auto-detection flows deterministically.
        /// </summary>
        public static PlatformDetectorScope ResetForDetection()
        {
            return new PlatformDetectorScope(ApplyResetDefaults);
        }

        /// <summary>
        /// Resets detection and forces the unity flag to the provided value.
        /// </summary>
        public static PlatformDetectorScope OverrideFlags(bool unity)
        {
            return new PlatformDetectorScope(() =>
            {
                ApplyResetDefaults();
                PlatformAutoDetector.TestHooks.SetFlags(isRunningOnUnity: unity);
                SetAutoDetectionsDone(true);
            });
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
            PlatformAutoDetector.TestHooks.SetFlags(
                isRunningOnMono,
                isRunningOnClr4,
                isRunningOnUnity,
                isPortableFramework,
                isUnityNative,
                isUnityIl2Cpp
            );
        }

        public static void SetAotValue(bool? value)
        {
            PlatformAutoDetector.TestHooks.SetRunningOnAot(value);
        }

        public static IDisposable OverrideAotProbe(Func<bool> probe)
        {
            // SetAotProbeOverride atomically sets the probe and resets the cached state
            PlatformAutoDetector.TestHooks.SetAotProbeOverride(probe);
            return new AotProbeOverrideHandle();
        }

        public static IDisposable OverrideAssemblyEnumeration(Func<Assembly[]> provider)
        {
            PlatformAutoDetector.TestHooks.SetAssemblyEnumerationOverride(provider);
            return new AssemblyEnumerationOverrideHandle();
        }

        public static Assembly[] EmptyAssemblyProvider()
        {
            return Array.Empty<Assembly>();
        }

        public static string DescribeCurrentState()
        {
            PlatformAutoDetector.PlatformDetectorSnapshot snapshot =
                PlatformAutoDetector.TestHooks.CaptureState();
            string aotOverrideDesc = snapshot.AotProbeOverride is null ? "null" : "set";
            return $"Unity={snapshot.IsRunningOnUnity}, UnityNative={snapshot.IsUnityNative}, UnityIl2Cpp={snapshot.IsUnityIl2Cpp}, Mono={snapshot.IsRunningOnMono}, Clr4={snapshot.IsRunningOnClr4}, Portable={snapshot.IsPortableFramework}, AutoDone={snapshot.AutoDetectionsDone}, AotCached={snapshot.RunningOnAotCache?.ToString() ?? "null"}, AotOverride={aotOverrideDesc}, UnityOverride={snapshot.UnityDetectionOverride?.ToString() ?? "null"}";
        }

        /// <summary>
        /// Captures the current detector state for diagnostic or verification purposes.
        /// </summary>
        public static PlatformAutoDetector.PlatformDetectorSnapshot CaptureSnapshot()
        {
            return PlatformAutoDetector.TestHooks.CaptureState();
        }

        /// <summary>
        /// Sets the AOT probe override directly. This is intended for low-level
        /// concurrency tests that cannot use the disposable <see cref="OverrideAotProbe"/> pattern.
        /// For typical tests, prefer <see cref="OverrideAotProbe"/> instead.
        /// </summary>
        public static void SetAotProbeOverrideDirect(Func<bool> probe)
        {
            PlatformAutoDetector.TestHooks.SetAotProbeOverride(probe);
        }

        public static string DescribeAssemblyEnumerationOverride()
        {
            Func<Assembly[]> provider =
                PlatformAutoDetector.TestHooks.GetAssemblyEnumerationOverride();
            if (provider == null)
            {
                return "AssemblyOverride=<null>";
            }

            Assembly[] assemblies = provider() ?? Array.Empty<Assembly>();
            string[] names = assemblies
                .Where(a => a != null)
                .Select(a =>
                    $"{a.GetName().Name}(IsDynamic={a.IsDynamic}, Location={SafeGetLocation(a)})"
                )
                .ToArray();
            return $"AssemblyOverride=[{string.Join(", ", names)}]";
        }

        public void Dispose()
        {
            _overrideScope.Dispose();
        }

        public static void SetAutoDetectionsDone(bool value)
        {
            PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(value);
        }

        private static void ClearAssemblyEnumerationOverride()
        {
            PlatformAutoDetector.TestHooks.SetAssemblyEnumerationOverride(null);
        }

        private static string SafeGetLocation(Assembly assembly)
        {
            try
            {
                return assembly.IsDynamic ? "<dynamic>" : assembly.Location;
            }
            catch (NotSupportedException)
            {
                return "<unsupported>";
            }
        }

        private sealed class AotProbeOverrideHandle : IDisposable
        {
            public void Dispose()
            {
                PlatformAutoDetector.TestHooks.SetAotProbeOverride(null);
            }
        }

        private sealed class AssemblyEnumerationOverrideHandle : IDisposable
        {
            public void Dispose()
            {
                ClearAssemblyEnumerationOverride();
            }
        }
    }
}
