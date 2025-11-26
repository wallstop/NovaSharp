namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PlatformAutoDetectorTests
    {
        [Test]
        public void GetDefaultPlatformReturnsLimitedAccessorWhenUnityFlagged()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: true);

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();

            Assert.That(platform, Is.TypeOf<LimitedPlatformAccessor>());
        }

        [Test]
        public void GetDefaultScriptLoaderDetectsUnityAssemblies()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            UnityAssemblyProbe.EnsureLoaded();

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();

            Assert.That(loader, Is.TypeOf<UnityAssetsScriptLoader>());
            Assert.That(PlatformAutoDetector.IsRunningOnUnity, Is.True);
        }

        [Test]
        public void GetDefaultPlatformReturnsDotNetCoreAccessorWhenUnityNotDetected()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: false);

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();

            Assert.That(platform, Is.TypeOf<DotNetCorePlatformAccessor>());
        }

        [Test]
        public void GetDefaultScriptLoaderReturnsFileSystemLoaderWhenUnityNotDetected()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: false);

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();

            Assert.That(loader, Is.TypeOf<FileSystemScriptLoader>());
        }

        [Test]
        public void IsRunningOnAotUsesCachedValueAfterProbe()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            bool initialProbe = PlatformAutoDetector.IsRunningOnAot;
            Assert.That(initialProbe, Is.False);

            PlatformDetectorScope.SetAotValue(true);

            bool cached = PlatformAutoDetector.IsRunningOnAot;

            Assert.That(cached, Is.True);
        }

        [Test]
        public void SetFlagsUpdatesUnityIndicators()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            PlatformDetectorScope.SetFlags(
                isRunningOnMono: true,
                isRunningOnClr4: true,
                isRunningOnUnity: true,
                isUnityNative: true,
                isUnityIl2Cpp: true
            );

            Assert.Multiple(() =>
            {
                Assert.That(PlatformAutoDetector.IsRunningOnMono, Is.True);
                Assert.That(PlatformAutoDetector.IsRunningOnClr4, Is.True);
                Assert.That(PlatformAutoDetector.IsRunningOnUnity, Is.True);
                Assert.That(PlatformAutoDetector.IsUnityNative, Is.True);
                Assert.That(PlatformAutoDetector.IsUnityIl2Cpp, Is.True);
            });
        }

        private sealed class PlatformDetectorScope : IDisposable
        {
            private readonly PlatformAutoDetector.PlatformDetectorSnapshot _snapshot;

            private PlatformDetectorScope()
            {
                _snapshot = PlatformAutoDetector.TestHooks.CaptureState();
            }

            public static PlatformDetectorScope ResetForDetection()
            {
                PlatformDetectorScope scope = new();
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
                return scope;
            }

            public static PlatformDetectorScope OverrideFlags(bool unity)
            {
                PlatformDetectorScope scope = ResetForDetection();
                PlatformAutoDetector.TestHooks.SetFlags(isRunningOnUnity: unity);
                SetAutoDetectionsDone(true);
                return scope;
            }

            public static void SetAotValue(bool? value)
            {
                PlatformAutoDetector.TestHooks.SetRunningOnAot(value);
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

            public void Dispose()
            {
                PlatformAutoDetector.TestHooks.RestoreState(_snapshot);
            }

            private static void SetAutoDetectionsDone(bool value)
            {
                PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(value);
            }
        }

        private static class UnityAssemblyProbe
        {
            private static bool IsLoaded;

            public static void EnsureLoaded()
            {
                if (IsLoaded)
                {
                    return;
                }

                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                    new AssemblyName("UnityEngine.AutoDetectorProbeAssembly"),
                    AssemblyBuilderAccess.Run
                );
                ModuleBuilder module = assembly.DefineDynamicModule("NovaSharpUnityProbe");
                module
                    .DefineType(
                        "UnityEngine.AutoDetectorProbe",
                        TypeAttributes.Public | TypeAttributes.Class
                    )
                    .CreateTypeInfo();

                IsLoaded = true;
            }
        }
    }
}
