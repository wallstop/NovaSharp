namespace NovaSharp.Tests.TestInfrastructure.Scopes
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Platforms;

    /// <summary>
    /// Provides disposable helpers for overriding platform detection flags in tests.
    /// </summary>
    internal sealed class PlatformDetectorOverrideScope : IDisposable
    {
        private readonly PlatformAutoDetector.PlatformDetectorSnapshot _snapshot;
        private readonly Action _additionalRestore;
        private bool _disposed;

        private PlatformDetectorOverrideScope(Action overrides, Action additionalRestore = null)
        {
            ArgumentNullException.ThrowIfNull(overrides);
            _additionalRestore = additionalRestore;
            _snapshot = PlatformAutoDetector.TestHooks.CaptureState();
            try
            {
                overrides();
            }
            catch
            {
                PlatformAutoDetector.TestHooks.RestoreState(_snapshot);
                _additionalRestore?.Invoke();
                throw;
            }
        }

        /// <summary>
        /// Creates a scope that applies custom overrides to the platform detector.
        /// </summary>
        public static PlatformDetectorOverrideScope Apply(
            Action overrides,
            Action additionalRestore = null
        )
        {
            return new PlatformDetectorOverrideScope(overrides, additionalRestore);
        }

        /// <summary>
        /// Forces the detector to use the desktop file system loader rather than Unity paths.
        /// </summary>
        public static PlatformDetectorOverrideScope ForceFileSystemLoader()
        {
            return Apply(() =>
            {
                PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(false);
                PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(false);
            });
        }

        /// <summary>
        /// Forces the desktop platform accessor and Unity detection flags.
        /// </summary>
        public static PlatformDetectorOverrideScope ForceDesktopPlatform()
        {
            ScriptPlatformScope platformScope = null;
            return Apply(
                () =>
                {
                    platformScope = ScriptPlatformScope.Override(new DotNetCorePlatformAccessor());
                    PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(false);
                    PlatformAutoDetector.TestHooks.SetFlags(
                        isRunningOnUnity: false,
                        isUnityNative: false,
                        isUnityIl2Cpp: false
                    );
                    PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(true);
                },
                () =>
                {
                    platformScope?.Dispose();
                }
            );
        }

        /// <summary>
        /// Sets the cached RunningOnAot flag for the duration of the scope.
        /// </summary>
        public static PlatformDetectorOverrideScope SetRunningOnAot(bool? value)
        {
            return Apply(() => PlatformAutoDetector.TestHooks.SetRunningOnAot(value));
        }

        /// <summary>
        /// Overrides the platform detector flags for the duration of the scope.
        /// </summary>
        public static PlatformDetectorOverrideScope SetPlatformFlags(
            bool? unity = null,
            bool? unityNative = null,
            bool? mono = null,
            bool? portable = null,
            bool? clr4 = null,
            bool? aot = null,
            bool autoDetectionsDone = true
        )
        {
            return Apply(() =>
            {
                PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(null);
                PlatformAutoDetector.TestHooks.SetFlags(
                    isRunningOnUnity: unity,
                    isUnityNative: unityNative,
                    isRunningOnMono: mono,
                    isPortableFramework: portable,
                    isRunningOnClr4: clr4
                );
                if (aot.HasValue)
                {
                    PlatformAutoDetector.TestHooks.SetRunningOnAot(aot.Value);
                }

                PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(autoDetectionsDone);
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            PlatformAutoDetector.TestHooks.RestoreState(_snapshot);
            _additionalRestore?.Invoke();
            _disposed = true;
        }
    }
}
