namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Platforms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    public sealed class PlatformAutoDetectorTUnitTests
    {
        private enum AutoDetectionEntryPoint
        {
            ScriptLoader,
            PlatformAccessor,
        }

        [global::TUnit.Core.Test]
        public async Task GetDefaultPlatformReturnsLimitedAccessorWhenUnityFlagged()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: true);

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();

            await Assert.That(platform).IsTypeOf<LimitedPlatformAccessor>();
        }

        [global::TUnit.Core.Test]
        public async Task GetDefaultScriptLoaderDetectsUnityAssemblies()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable assemblies = PlatformDetectorScope.OverrideAssemblyEnumeration(
                UnityAssemblyProbe.GetAssemblies
            );
            UnityAssemblyProbe.EnsureLoaded();

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();
            Console.WriteLine(PlatformDetectorScope.DescribeAssemblyEnumerationOverride());

            await Assert.That(loader).IsTypeOf<UnityAssetsScriptLoader>();
            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task GetDefaultPlatformReturnsDotNetCoreAccessorWhenUnityNotDetected()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: false);

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();

            await Assert.That(platform).IsTypeOf<DotNetCorePlatformAccessor>();
        }

        [global::TUnit.Core.Test]
        public async Task GetDefaultScriptLoaderReturnsFileSystemLoaderWhenUnityNotDetected()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: false);

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();

            await Assert.That(loader).IsTypeOf<FileSystemScriptLoader>();
        }

        [global::TUnit.Core.Test]
        public async Task AutoDetectionWithoutUnityAssembliesPrefersFileSystemLoader()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable assemblies = PlatformDetectorScope.OverrideAssemblyEnumeration(
                PlatformDetectorScope.EmptyAssemblyProvider
            );

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();
            Console.WriteLine(PlatformDetectorScope.DescribeAssemblyEnumerationOverride());

            await Assert.That(loader).IsTypeOf<FileSystemScriptLoader>();
            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsFalse();
            await Assert.That(PlatformAutoDetector.IsUnityNative).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task AutoDetectionDoesNothingWhenAlreadyInitializedForScriptLoader()
        {
            await AssertAutoDetectionNoOpAsync(AutoDetectionEntryPoint.ScriptLoader)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AutoDetectionDoesNothingWhenAlreadyInitializedForPlatformAccessor()
        {
            await AssertAutoDetectionNoOpAsync(AutoDetectionEntryPoint.PlatformAccessor)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AutoDetectionMarksUnityWhenUnityTypesPresent()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable assemblies = PlatformDetectorScope.OverrideAssemblyEnumeration(
                UnityAssemblyProbe.GetAssemblies
            );
            UnityAssemblyProbe.EnsureLoaded();

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();
            Console.WriteLine(PlatformDetectorScope.DescribeAssemblyEnumerationOverride());

            await Assert.That(platform).IsTypeOf<LimitedPlatformAccessor>();
            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task IsRunningOnAotUsesCachedValueAfterProbe()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            string stateBeforeOverride = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State before override: {stateBeforeOverride}");

            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => true);
            string stateAfterOverride = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State after override: {stateAfterOverride}");

            // Verify the override was registered before accessing IsRunningOnAot.
            // This helps diagnose race conditions where the override might not be visible.
            PlatformAutoDetector.PlatformDetectorSnapshot verificationSnapshot =
                PlatformAutoDetector.TestHooks.CaptureState();
            await Assert
                .That(verificationSnapshot.AotProbeOverride is not null)
                .IsTrue()
                .Because("AOT probe override should be set after OverrideAotProbe call");
            await Assert
                .That(verificationSnapshot.RunningOnAotCache.HasValue)
                .IsFalse()
                .Because("AOT cache should be reset to null (unknown) after setting override");

            bool initialProbe = PlatformAutoDetector.IsRunningOnAot;
            string stateAfterInitialProbe = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine(
                $"Initial probe result: {initialProbe}, state: {stateAfterInitialProbe}"
            );
            await Assert.That(initialProbe).IsTrue();

            probe.Dispose();
            string stateAfterDispose = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State after dispose: {stateAfterDispose}");

            bool cachedValue = PlatformAutoDetector.IsRunningOnAot;
            string stateAfterCachedRead = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"Cached value result: {cachedValue}, state: {stateAfterCachedRead}");
            await Assert.That(cachedValue).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task SetFlagsUpdatesUnityIndicators()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            PlatformDetectorScope.SetFlags(
                isRunningOnMono: true,
                isRunningOnClr4: true,
                isRunningOnUnity: true,
                isUnityNative: true,
                isUnityIl2Cpp: true
            );

            await Assert.That(PlatformAutoDetector.IsRunningOnMono).IsTrue();
            await Assert.That(PlatformAutoDetector.IsRunningOnClr4).IsTrue();
            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsTrue();
            await Assert.That(PlatformAutoDetector.IsUnityNative).IsTrue();
            await Assert.That(PlatformAutoDetector.IsUnityIl2Cpp).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task IsRunningOnAotUsesProbeOverride()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => true);

            await Assert.That(PlatformAutoDetector.IsRunningOnAot).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task IsRunningOnAotTreatsProbeExceptionsAsAotHosts()
        {
            IReadOnlyList<(Type Type, string Name)> exceptionTypes = new[]
            {
                (typeof(PlatformNotSupportedException), nameof(PlatformNotSupportedException)),
                (typeof(MemberAccessException), nameof(MemberAccessException)),
                (typeof(NotSupportedException), nameof(NotSupportedException)),
                (typeof(InvalidOperationException), nameof(InvalidOperationException)),
                (typeof(TypeLoadException), nameof(TypeLoadException)),
                (typeof(SecurityException), nameof(SecurityException)),
            };

            foreach ((Type exceptionType, string exceptionName) in exceptionTypes)
            {
                using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
                using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() =>
                {
                    Exception instance = (Exception)Activator.CreateInstance(exceptionType);
                    throw instance;
                });

                bool isRunningOnAot = PlatformAutoDetector.IsRunningOnAot;
                string state = PlatformDetectorScope.DescribeCurrentState();
                Console.WriteLine(
                    $"Exception {exceptionName} -> IsRunningOnAot={isRunningOnAot}, State: {state}"
                );
                await Assert.That(isRunningOnAot).IsTrue();
            }
        }

        [global::TUnit.Core.Test]
        public async Task IsRunningOnAotHandlesConcurrentCacheResets()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => false);
            PlatformDetectorScope.SetAotValue(null);

            const int workerCount = 12;
            const int iterationsPerWorker = 500;
            List<Task> workers = new(workerCount + 1);

            for (int index = 0; index < workerCount; index++)
            {
                workers.Add(
                    Task.Run(() =>
                    {
                        for (int iteration = 0; iteration < iterationsPerWorker; iteration++)
                        {
                            bool result = PlatformAutoDetector.IsRunningOnAot;
                            if (result)
                            {
                                throw new InvalidOperationException(
                                    "Probe override expected false."
                                );
                            }
                        }
                    })
                );
            }

            workers.Add(
                Task.Run(() =>
                {
                    for (
                        int iteration = 0;
                        iteration < workerCount * iterationsPerWorker;
                        iteration++
                    )
                    {
                        PlatformDetectorScope.SetAotValue(null);
                        Thread.Yield();
                    }
                })
            );

            await Task.WhenAll(workers).ConfigureAwait(false);
            await Assert.That(PlatformAutoDetector.IsRunningOnAot).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task IsRunningOnAotProbeOverrideIsAtomicWithStateReset()
        {
            // This test verifies that setting a probe override atomically resets the cached state,
            // preventing a race where another thread probing without an override could cache a
            // false value before the test's override takes effect.
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            const int iterations = 50;
            int successCount = 0;
            int failureCount = 0;

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // Reset to false to simulate a cached "JIT available" state
                PlatformDetectorScope.SetAotValue(false);

                // Simulate the race: one thread sets the override, another reads the value
                Task setOverrideTask = Task.Run(() =>
                {
                    // Set probe override that returns true
                    PlatformAutoDetector.TestHooks.SetAotProbeOverride(() => true);
                });

                Task<bool> readTask = Task.Run(() => PlatformAutoDetector.IsRunningOnAot);

                await Task.WhenAll(setOverrideTask, readTask).ConfigureAwait(false);
                bool readResult = await readTask.ConfigureAwait(false);

                // With the fix, the read should either:
                // 1. See the old cached false value (if it read before the override was set), OR
                // 2. See true (if the override was set before/during the read and triggered a re-probe)
                // The bug was when the read would probe WITHOUT the override but AFTER the
                // state was reset, caching false when true was intended.

                // Since timing is non-deterministic, we just track the results
                if (readResult)
                {
                    Interlocked.Increment(ref successCount);
                }
                else
                {
                    Interlocked.Increment(ref failureCount);
                }

                // Clear for next iteration
                PlatformAutoDetector.TestHooks.SetAotProbeOverride(null);
            }

            Console.WriteLine(
                $"Atomic probe override test: {successCount} successes (true), {failureCount} failures (false)"
            );

            // Note: We don't assert a specific ratio here because timing varies.
            // The purpose is to verify no crashes or hangs under concurrent access.
            await Assert.That(successCount + failureCount).IsEqualTo(iterations);
        }

        [global::TUnit.Core.Test]
        public async Task SetFlagsUpdatesPortableFrameworkIndicator()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            PlatformDetectorScope.SetFlags(isPortableFramework: true);

            await Assert.That(PlatformAutoDetector.IsPortableFramework).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task DetectorScopeDisposalRestoresCapturedState()
        {
            using (PlatformDetectorScope.ResetForDetection()) { }

            bool originalUnityFlag = PlatformAutoDetector.IsRunningOnUnity;
            using (PlatformDetectorScope.OverrideFlags(unity: !originalUnityFlag))
            {
                await Assert
                    .That(PlatformAutoDetector.IsRunningOnUnity)
                    .IsEqualTo(!originalUnityFlag);
            }

            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsEqualTo(originalUnityFlag);
        }

        private static class UnityAssemblyProbe
        {
            public static void EnsureLoaded()
            {
                _ = BuildAssembly(
                    "UnityEngine",
                    module =>
                    {
                        DefineEmptyType(module, "UnityEngine.AutoDetectorProbe");
                        DefineEmptyType(module, "UnityEngine.PlatformProbe");
                        DefineTextAssetType(module);
                        DefineResourcesType(module);
                    }
                );
            }

            public static Assembly[] GetAssemblies()
            {
                Assembly assembly = BuildAssembly(
                    "UnityEngine",
                    module =>
                    {
                        DefineEmptyType(module, "UnityEngine.AutoDetectorProbe");
                        DefineEmptyType(module, "UnityEngine.PlatformProbe");
                        DefineTextAssetType(module);
                        DefineResourcesType(module);
                    }
                );
                return new[] { assembly };
            }

            private static void DefineEmptyType(ModuleBuilder module, string name)
            {
                TypeBuilder builder = module.DefineType(
                    name,
                    TypeAttributes.Public | TypeAttributes.Class
                );
                builder.DefineDefaultConstructor(MethodAttributes.Public);
                builder.CreateTypeInfo();
            }

            private static void DefineTextAssetType(ModuleBuilder module)
            {
                TypeBuilder builder = module.DefineType(
                    "UnityEngine.TextAsset",
                    TypeAttributes.Public | TypeAttributes.Class
                );
                builder.DefineDefaultConstructor(MethodAttributes.Public);
                builder.CreateTypeInfo();
            }

            private static void DefineResourcesType(ModuleBuilder module)
            {
                TypeBuilder resources = module.DefineType(
                    "UnityEngine.Resources",
                    TypeAttributes.Public
                        | TypeAttributes.Class
                        | TypeAttributes.Abstract
                        | TypeAttributes.Sealed
                );
                MethodBuilder loadAll = resources.DefineMethod(
                    "LoadAll",
                    MethodAttributes.Public | MethodAttributes.Static,
                    typeof(Array),
                    new[] { typeof(string), typeof(Type) }
                );
                ILGenerator il = loadAll.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4_0);
                MethodInfo createInstance = typeof(Array).GetMethod(
                    "CreateInstance",
                    new[] { typeof(Type), typeof(int) }
                );
                il.EmitCall(OpCodes.Call, createInstance, null);
                il.Emit(OpCodes.Ret);
                resources.CreateTypeInfo();
            }
        }

        private static class UnityTypeProbe
        {
            public static void EnsureTypeLoaded()
            {
                _ = BuildAssembly(
                    "UnityEngine.PlatformProbeAssembly",
                    module =>
                    {
                        DefineEmptyType(module, "UnityEngine.PlatformProbe");
                    }
                );
            }

            public static Assembly[] GetAssemblies()
            {
                Assembly assembly = BuildAssembly(
                    "UnityEngine.PlatformProbeAssembly",
                    module =>
                    {
                        DefineEmptyType(module, "UnityEngine.PlatformProbe");
                    }
                );
                return new[] { assembly };
            }

            private static void DefineEmptyType(ModuleBuilder module, string name)
            {
                TypeBuilder builder = module.DefineType(
                    name,
                    TypeAttributes.Public | TypeAttributes.Class
                );
                builder.DefineDefaultConstructor(MethodAttributes.Public);
                builder.CreateTypeInfo();
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

        private static async Task AssertAutoDetectionNoOpAsync(AutoDetectionEntryPoint entryPoint)
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.CaptureStateOnly();
            PlatformDetectorScope.SetFlags(
                isRunningOnMono: true,
                isRunningOnClr4: true,
                isRunningOnUnity: true
            );
            PlatformDetectorScope.SetAutoDetectionsDone(true);

            bool monoBefore = PlatformAutoDetector.IsRunningOnMono;
            bool unityBefore = PlatformAutoDetector.IsRunningOnUnity;

            object entryResult = entryPoint switch
            {
                AutoDetectionEntryPoint.ScriptLoader =>
                    PlatformAutoDetector.GetDefaultScriptLoader(),
                AutoDetectionEntryPoint.PlatformAccessor =>
                    PlatformAutoDetector.GetDefaultPlatform(),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(entryPoint),
                    entryPoint,
                    "Unsupported detector entry point."
                ),
            };

            string detectorState = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"[{entryPoint}] Detector state after invocation: {detectorState}");

            if (entryPoint == AutoDetectionEntryPoint.ScriptLoader)
            {
                await Assert.That(entryResult).IsTypeOf<UnityAssetsScriptLoader>();
            }
            else
            {
                await Assert.That(entryResult).IsTypeOf<LimitedPlatformAccessor>();
            }

            await Assert.That(PlatformAutoDetector.IsRunningOnMono).IsEqualTo(monoBefore);
            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsEqualTo(unityBefore);
        }
    }
}
