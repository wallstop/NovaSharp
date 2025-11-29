namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    [PlatformDetectorIsolation]
    public sealed class PlatformAutoDetectorTests
    {
        [Test]
        public void GetDefaultPlatformReturnsLimitedAccessorWhenUnityFlagged()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: true);

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();

            Assert.That(platform, Is.TypeOf<LimitedPlatformAccessor>());
        }

        [Test, Order(2)]
        public void GetDefaultScriptLoaderDetectsUnityAssemblies()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable assemblies = PlatformDetectorScope.OverrideAssemblyEnumeration(
                UnityAssemblyProbe.GetAssemblies
            );
            UnityAssemblyProbe.EnsureLoaded();

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();
            string assemblyState = PlatformDetectorScope.DescribeAssemblyEnumerationOverride();

            Assert.Multiple(() =>
            {
                Assert.That(
                    loader,
                    Is.TypeOf<UnityAssetsScriptLoader>(),
                    $"Unity loader was not selected. {assemblyState}"
                );
                Assert.That(
                    PlatformAutoDetector.IsRunningOnUnity,
                    Is.True,
                    $"Unity flag not updated after detection. {assemblyState}"
                );
            });
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

        [Test, Order(1)]
        public void AutoDetectionWithoutUnityAssembliesPrefersFileSystemLoader()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable assemblies = PlatformDetectorScope.OverrideAssemblyEnumeration(
                PlatformDetectorScope.EmptyAssemblyProvider
            );

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();
            string assemblyState = PlatformDetectorScope.DescribeAssemblyEnumerationOverride();

            Assert.Multiple(() =>
            {
                Assert.That(
                    loader,
                    Is.TypeOf<FileSystemScriptLoader>(),
                    $"Unity assemblies reported when none were provided. {assemblyState}"
                );
                Assert.That(
                    PlatformAutoDetector.IsRunningOnUnity,
                    Is.False,
                    $"Unity flag unexpectedly set. {assemblyState}"
                );
                Assert.That(
                    PlatformAutoDetector.IsUnityNative,
                    Is.False,
                    $"UnityNative flag unexpectedly set. {assemblyState}"
                );
            });
        }

        [TestCase(
            true,
            TestName = nameof(AutoDetectionDoesNothingWhenAlreadyInitialized) + "_ScriptLoader"
        )]
        [TestCase(
            false,
            TestName = nameof(AutoDetectionDoesNothingWhenAlreadyInitialized) + "_PlatformAccessor"
        )]
        public void AutoDetectionDoesNothingWhenAlreadyInitialized(bool useScriptLoader)
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.CaptureStateOnly();

            PlatformDetectorScope.SetFlags(
                isRunningOnMono: true,
                isRunningOnClr4: true,
                isRunningOnUnity: true
            );
            PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(true);

            bool monoBefore = PlatformAutoDetector.IsRunningOnMono;
            bool unityBefore = PlatformAutoDetector.IsRunningOnUnity;

            object entryResult = useScriptLoader
                ? PlatformAutoDetector.GetDefaultScriptLoader()
                : PlatformAutoDetector.GetDefaultPlatform();

            string scenario = useScriptLoader ? "ScriptLoader" : "PlatformAccessor";
            string detectorState = PlatformDetectorScope.DescribeCurrentState();
            TestContext.WriteLine($"[{scenario}] Detector state after invocation: {detectorState}");

            Assert.Multiple(() =>
            {
                if (useScriptLoader)
                {
                    Assert.That(
                        entryResult,
                        Is.InstanceOf<UnityAssetsScriptLoader>(),
                        $"Unexpected loader when reusing detection via {scenario}. {detectorState}"
                    );
                }
                else
                {
                    Assert.That(
                        entryResult,
                        Is.InstanceOf<LimitedPlatformAccessor>(),
                        $"Unexpected platform accessor when reusing detection via {scenario}. {detectorState}"
                    );
                }

                Assert.That(
                    PlatformAutoDetector.IsRunningOnMono,
                    Is.EqualTo(monoBefore),
                    $"Mono flag mutated while invoking {scenario}. {detectorState}"
                );
                Assert.That(
                    PlatformAutoDetector.IsRunningOnUnity,
                    Is.EqualTo(unityBefore),
                    $"Unity flag mutated while invoking {scenario}. {detectorState}"
                );
            });
        }

        [Test, Order(3)]
        public void AutoDetectionMarksUnityWhenUnityTypesPresent()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable assemblies = PlatformDetectorScope.OverrideAssemblyEnumeration(
                UnityTypeProbe.GetAssemblies
            );
            UnityTypeProbe.EnsureTypeLoaded();

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();
            string assemblyState = PlatformDetectorScope.DescribeAssemblyEnumerationOverride();

            Assert.Multiple(() =>
            {
                Assert.That(
                    platform,
                    Is.TypeOf<LimitedPlatformAccessor>(),
                    $"Limited accessor not selected. {assemblyState}"
                );
                Assert.That(
                    PlatformAutoDetector.IsRunningOnUnity,
                    Is.True,
                    $"Unity flag not updated. {assemblyState}"
                );
            });
        }

        [Test]
        public void IsRunningOnAotUsesCachedValueAfterProbe()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => true);

            bool initialProbe = PlatformAutoDetector.IsRunningOnAot;
            Assert.That(initialProbe, Is.True);

            probe.Dispose();

            PlatformAutoDetector.TestHooks.SetAotProbeOverride(() =>
            {
                Assert.Fail("Cached AOT detection should not re-run the probe.");
                return false;
            });
            try
            {
                TestContext.WriteLine(
                    "[AOT:CachedProbe] state before cached read -> "
                        + PlatformDetectorScope.DescribeCurrentState()
                );
                bool cached = PlatformAutoDetector.IsRunningOnAot;
                Assert.That(cached, Is.True);
            }
            finally
            {
                PlatformAutoDetector.TestHooks.SetAotProbeOverride(null);
            }
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

        [Test]
        public void IsRunningOnAotUsesProbeOverride()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => true);

            Assert.That(PlatformAutoDetector.IsRunningOnAot, Is.True);
        }

        [TestCase(typeof(PlatformNotSupportedException))]
        [TestCase(typeof(MemberAccessException))]
        [TestCase(typeof(NotSupportedException))]
        [TestCase(typeof(InvalidOperationException))]
        [TestCase(typeof(TypeLoadException))]
        [TestCase(typeof(System.Security.SecurityException))]
        public void IsRunningOnAotTreatsProbeExceptionsAsAotHosts(Type exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() =>
            {
                Exception instance = (Exception)Activator.CreateInstance(exceptionType);
                throw instance;
            });
            PlatformAutoDetector.TestHooks.SetRunningOnAot(null);
            TestContext.WriteLine(
                $"[AOT:{exceptionType.Name}] before probe -> {PlatformDetectorScope.DescribeCurrentState()}, ProbeOverrideNull={PlatformAutoDetector.TestHooks.GetAotProbeOverride() == null}"
            );

            bool isRunningOnAot = false;
            for (int attempt = 0; attempt < 3 && !isRunningOnAot; attempt++)
            {
                PlatformAutoDetector.TestHooks.SetRunningOnAot(null);
                isRunningOnAot = PlatformAutoDetector.IsRunningOnAot;
            }

            Assert.That(
                isRunningOnAot,
                Is.True,
                $"AOT detection did not treat {exceptionType.Name} as recoverable. {PlatformDetectorScope.DescribeCurrentState()}"
            );
            TestContext.WriteLine(
                $"[AOT:{exceptionType.Name}] after probe -> {PlatformDetectorScope.DescribeCurrentState()}"
            );
        }

        [Test]
        public void IsRunningOnAotHandlesConcurrentCacheResets()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => false);
            PlatformDetectorScope.SetAotValue(null);

            const int workerCount = 12;
            const int iterationsPerWorker = 500;
            Task[] workers = new Task[workerCount + 1];

            for (int index = 0; index < workerCount; index++)
            {
                workers[index] = Task.Run(() =>
                {
                    for (int iteration = 0; iteration < iterationsPerWorker; iteration++)
                    {
                        bool result = PlatformAutoDetector.IsRunningOnAot;
                        if (result)
                        {
                            throw new InvalidOperationException("Probe override expected false.");
                        }
                    }
                });
            }

            workers[workerCount] = Task.Run(() =>
            {
                int resetIterations = workerCount * iterationsPerWorker;
                for (int iteration = 0; iteration < resetIterations; iteration++)
                {
                    PlatformDetectorScope.SetAotValue(null);
                    Thread.Yield();
                }
            });

            TestContext.WriteLine(
                $"[AOT:Concurrent] workers={workerCount}, iterations={iterationsPerWorker}"
            );

            Assert.DoesNotThrow(() => Task.WaitAll(workers));
            Assert.That(PlatformAutoDetector.IsRunningOnAot, Is.False);
        }

        [Test]
        public void SetFlagsUpdatesPortableFrameworkIndicator()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            PlatformDetectorScope.SetFlags(isPortableFramework: true);

            Assert.That(PlatformAutoDetector.IsPortableFramework, Is.True);
        }

        [Test]
        public void DetectorScopeDisposalRestoresCapturedState()
        {
            using (PlatformDetectorScope.ResetForDetection()) { }

            bool originalUnityFlag = PlatformAutoDetector.IsRunningOnUnity;
            PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(
                unity: !originalUnityFlag
            );
            try
            {
                Assert.That(PlatformAutoDetector.IsRunningOnUnity, Is.EqualTo(!originalUnityFlag));
            }
            finally
            {
                scope.Dispose();
            }

            Assert.That(PlatformAutoDetector.IsRunningOnUnity, Is.EqualTo(originalUnityFlag));
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
                return scope;
            }

            public static PlatformDetectorScope CaptureStateOnly()
            {
                return new PlatformDetectorScope();
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
                PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(null);
                PlatformAutoDetector.TestHooks.SetFlags(
                    isRunningOnMono,
                    isRunningOnClr4,
                    isRunningOnUnity,
                    isPortableFramework,
                    isUnityNative,
                    isUnityIl2Cpp
                );
            }

            public static IDisposable OverrideAotProbe(Func<bool> probe)
            {
                PlatformAutoDetector.TestHooks.SetAotProbeOverride(probe);
                PlatformAutoDetector.TestHooks.SetRunningOnAot(null);
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

            public void Dispose()
            {
                PlatformAutoDetector.TestHooks.RestoreState(_snapshot);
            }

            public static string DescribeCurrentState()
            {
                PlatformAutoDetector.PlatformDetectorSnapshot snapshot =
                    PlatformAutoDetector.TestHooks.CaptureState();
                return $"Unity={snapshot.IsRunningOnUnity}, UnityNative={snapshot.IsUnityNative}, UnityIl2Cpp={snapshot.IsUnityIl2Cpp}, Mono={snapshot.IsRunningOnMono}, Clr4={snapshot.IsRunningOnClr4}, Portable={snapshot.IsPortableFramework}, AutoDone={snapshot.AutoDetectionsDone}, AotCached={snapshot.RunningOnAotCache?.ToString() ?? "null"}, UnityOverride={snapshot.UnityDetectionOverride?.ToString() ?? "null"}";
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

            private static void SetAutoDetectionsDone(bool value)
            {
                PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(value);
            }

            private static void ClearAssemblyEnumerationOverride()
            {
                PlatformAutoDetector.TestHooks.SetAssemblyEnumerationOverride(null);
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
        }

        private static class UnityAssemblyProbe
        {
            public static void EnsureLoaded()
            {
                _ = BuildAssembly(
                    "UnityEngine.AutoDetectorProbeAssembly",
                    builder =>
                    {
                        builder
                            .DefineType(
                                "UnityEngine.AutoDetectorProbe",
                                TypeAttributes.Public | TypeAttributes.Class
                            )
                            .CreateTypeInfo();
                    }
                );
            }

            public static Assembly[] GetAssemblies()
            {
                Assembly assembly = BuildAssembly(
                    "UnityEngine.AutoDetectorProbeAssembly",
                    builder =>
                    {
                        builder
                            .DefineType(
                                "UnityEngine.AutoDetectorProbe",
                                TypeAttributes.Public | TypeAttributes.Class
                            )
                            .CreateTypeInfo();
                    }
                );
                return new[] { assembly };
            }
        }

        private static class UnityTypeProbe
        {
            public static void EnsureTypeLoaded()
            {
                _ = BuildAssembly(
                    "UnityEngine.PlatformProbeAssembly",
                    builder =>
                    {
                        builder
                            .DefineType(
                                "UnityEngine.PlatformProbe",
                                TypeAttributes.Public | TypeAttributes.Class
                            )
                            .CreateTypeInfo();
                    }
                );
            }

            public static Assembly[] GetAssemblies()
            {
                Assembly assembly = BuildAssembly(
                    "UnityEngine.PlatformProbeAssembly",
                    builder =>
                    {
                        builder
                            .DefineType(
                                "UnityEngine.PlatformProbe",
                                TypeAttributes.Public | TypeAttributes.Class
                            )
                            .CreateTypeInfo();
                    }
                );
                return new[] { assembly };
            }
        }

        private static AssemblyBuilder BuildAssembly(string name, Action<ModuleBuilder> defineTypes)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(name),
                AssemblyBuilderAccess.RunAndCollect
            );
            ModuleBuilder module = assembly.DefineDynamicModule($"{name}.Module");
            defineTypes(module);
            return assembly;
        }
    }
}
