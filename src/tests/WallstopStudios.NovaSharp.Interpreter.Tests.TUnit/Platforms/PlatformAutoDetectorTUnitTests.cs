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
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
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
        public async Task IsRunningOnAotUsesProbeOverrideWithoutAffectingGlobalCache()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            // Pre-populate the global cache with false (JIT available).
            PlatformDetectorScope.SetAotValue(false);
            string stateBeforeOverride = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State before override: {stateBeforeOverride}");

            using (IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => true))
            {
                string stateAfterOverride = PlatformDetectorScope.DescribeCurrentState();
                Console.WriteLine($"State after override: {stateAfterOverride}");

                // Verify the override was registered in the current async flow.
                PlatformAutoDetector.PlatformDetectorSnapshot verificationSnapshot =
                    PlatformDetectorScope.CaptureSnapshot();
                await Assert
                    .That(verificationSnapshot.AotProbeOverride is not null)
                    .IsTrue()
                    .Because("AOT probe override should be set after OverrideAotProbe call");

                // Global cache should NOT be affected by setting a flow-local probe override.
                await Assert
                    .That(verificationSnapshot.RunningOnAotCache)
                    .IsEqualTo(false)
                    .Because(
                        "Global AOT cache should remain unchanged when setting flow-local probe override"
                    );

                // When probe override is active, it bypasses the cache and returns probe result.
                bool probeResult = PlatformAutoDetector.IsRunningOnAot;
                string stateAfterProbe = PlatformDetectorScope.DescribeCurrentState();
                Console.WriteLine($"Probe result: {probeResult}, state: {stateAfterProbe}");
                await Assert
                    .That(probeResult)
                    .IsTrue()
                    .Because("Probe override should return true, bypassing the cached false value");

                // Global cache should still be unchanged (false) because probe overrides are isolated.
                PlatformAutoDetector.PlatformDetectorSnapshot afterProbeSnapshot =
                    PlatformDetectorScope.CaptureSnapshot();
                await Assert
                    .That(afterProbeSnapshot.RunningOnAotCache)
                    .IsEqualTo(false)
                    .Because("Global cache should remain false even after probe returned true");
            }

            // After disposing probe, reads should use the global cache.
            string stateAfterDispose = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State after dispose: {stateAfterDispose}");

            bool cachedValue = PlatformAutoDetector.IsRunningOnAot;
            string stateAfterCachedRead = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"Cached value result: {cachedValue}, state: {stateAfterCachedRead}");
            await Assert
                .That(cachedValue)
                .IsFalse()
                .Because("After probe is disposed, global cache (false) should be used");
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
        [global::TUnit.Core.Arguments(true)]
        [global::TUnit.Core.Arguments(false)]
        public async Task IsRunningOnAotUsesProbeOverride(bool expectedValue)
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            string initialState = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"Initial state: {initialState}");

            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => expectedValue);
            string stateAfterOverride = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State after override: {stateAfterOverride}");

            bool actualValue = PlatformAutoDetector.IsRunningOnAot;
            string finalState = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine(
                $"Expected={expectedValue}, Actual={actualValue}, Final state: {finalState}"
            );

            await Assert
                .That(actualValue)
                .IsEqualTo(expectedValue)
                .Because($"Probe returned {expectedValue}, so IsRunningOnAot should match");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(PlatformNotSupportedException))]
        [global::TUnit.Core.Arguments(typeof(MemberAccessException))]
        [global::TUnit.Core.Arguments(typeof(NotSupportedException))]
        [global::TUnit.Core.Arguments(typeof(InvalidOperationException))]
        [global::TUnit.Core.Arguments(typeof(TypeLoadException))]
        [global::TUnit.Core.Arguments(typeof(SecurityException))]
        public async Task IsRunningOnAotTreatsProbeExceptionsAsAotHosts(Type exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            string initialState = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"Initial state: {initialState}");

            Type capturedExceptionType = exceptionType;
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() =>
            {
                Exception instance = (Exception)Activator.CreateInstance(capturedExceptionType);
                throw instance;
            });
            string stateAfterOverride = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State after setting probe override: {stateAfterOverride}");

            bool isRunningOnAot = PlatformAutoDetector.IsRunningOnAot;
            string finalState = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine(
                $"Exception {exceptionType.Name} -> IsRunningOnAot={isRunningOnAot}, Final state: {finalState}"
            );

            await Assert
                .That(isRunningOnAot)
                .IsTrue()
                .Because(
                    $"When AOT probe throws {exceptionType.Name}, the platform should be treated as AOT"
                );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(typeof(ArgumentException))]
        [global::TUnit.Core.Arguments(typeof(OutOfMemoryException))]
        [global::TUnit.Core.Arguments(typeof(NullReferenceException))]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Testing that exceptions propagate; we must catch all to verify which type was thrown"
        )]
        public async Task IsRunningOnAotPropagatesUnexpectedExceptions(Type exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            string initialState = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"Initial state: {initialState}");

            Type capturedExceptionType = exceptionType;
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() =>
            {
                Exception instance = (Exception)Activator.CreateInstance(capturedExceptionType);
                throw instance;
            });
            string stateAfterOverride = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine($"State after setting probe override: {stateAfterOverride}");

            Exception caught = null;
            try
            {
                _ = PlatformAutoDetector.IsRunningOnAot;
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            string finalState = PlatformDetectorScope.DescribeCurrentState();
            Console.WriteLine(
                $"Exception {exceptionType.Name} -> Caught: {caught?.GetType().Name ?? "none"}, Final state: {finalState}"
            );

            await Assert
                .That(caught)
                .IsNotNull()
                .Because(
                    $"When AOT probe throws unexpected {exceptionType.Name}, it should propagate"
                );
            await Assert
                .That(caught!.GetType())
                .IsEqualTo(exceptionType)
                .Because(
                    $"The propagated exception should be of the same type ({exceptionType.Name})"
                );
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
        public async Task IsRunningOnAotProbeOverrideIsIsolatedPerAsyncFlow()
        {
            // This test verifies that AOT probe overrides are isolated per async flow using AsyncLocal.
            // Setting a probe override in one thread does NOT affect reads in concurrent threads.
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            // Pre-populate the global cache with false (JIT available).
            PlatformDetectorScope.SetAotValue(false);

            const int iterations = 50;
            int readsSawFalse = 0;
            int readsSawTrue = 0;

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // One thread sets an override that returns true.
                Task setOverrideTask = Task.Run(() =>
                {
                    PlatformDetectorScope.SetAotProbeOverrideDirect(() => true);
                    // Read within the same flow should see the override.
                    bool result = PlatformAutoDetector.IsRunningOnAot;
                    if (!result)
                    {
                        throw new InvalidOperationException(
                            "Probe override should return true in the same async flow"
                        );
                    }

                    PlatformDetectorScope.SetAotProbeOverrideDirect(null);
                });

                // A concurrent thread reads without setting an override.
                // Due to AsyncLocal isolation, it should NOT see the override from the other thread.
                Task<bool> readTask = Task.Run(() => PlatformAutoDetector.IsRunningOnAot);

                await Task.WhenAll(setOverrideTask, readTask).ConfigureAwait(false);
                bool readResult = await readTask.ConfigureAwait(false);

                // The read should see the global cached value (false), not the other thread's override.
                if (readResult)
                {
                    Interlocked.Increment(ref readsSawTrue);
                }
                else
                {
                    Interlocked.Increment(ref readsSawFalse);
                }
            }

            Console.WriteLine(
                $"Async flow isolation test: {readsSawFalse} reads saw false (expected), {readsSawTrue} saw true (unexpected)"
            );

            // With AsyncLocal isolation, concurrent reads should ALWAYS see the global cache (false),
            // never the override from the other flow.
            await Assert
                .That(readsSawFalse)
                .IsEqualTo(iterations)
                .Because(
                    "Reads in a separate async flow should not see another flow's probe override"
                );
        }

        [global::TUnit.Core.Test]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Testing that Script creation does not throw; any exception is a failure"
        )]
        public async Task ExceptionThrowingProbeDoesNotAffectConcurrentScriptCreation()
        {
            // This test reproduces the original bug where a test setting an exception-throwing
            // probe would leak to other tests creating Script instances concurrently.
            // With AsyncLocal isolation, this should no longer happen.

            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            // Pre-populate the global cache so concurrent Script creation succeeds.
            PlatformDetectorScope.SetAotValue(false);

            const int iterations = 20;
            int scriptCreationsSucceeded = 0;
            int scriptCreationsFailed = 0;

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                // One task sets an exception-throwing probe override.
                Task exceptionProbeTask = Task.Run(() =>
                {
                    PlatformDetectorScope.SetAotProbeOverrideDirect(() =>
                    {
                        throw new ArgumentException("Simulated AOT probe failure");
                    });
                    using (
                        DeferredActionScope.Run(() =>
                            PlatformDetectorScope.SetAotProbeOverrideDirect(null)
                        )
                    )
                    {
                        try
                        {
                            // Access IsRunningOnAot to trigger the exception within this flow.
                            _ = PlatformAutoDetector.IsRunningOnAot;
                        }
                        catch (ArgumentException)
                        {
                            // Expected in this flow.
                        }
                    }
                });

                // A concurrent task creates a Script, which triggers AOT detection.
                Task scriptCreationTask = Task.Run(() =>
                {
                    try
                    {
                        Script script = new Script(CoreModulePresets.HardSandbox);
                        Interlocked.Increment(ref scriptCreationsSucceeded);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref scriptCreationsFailed);
                    }
                });

                await Task.WhenAll(exceptionProbeTask, scriptCreationTask).ConfigureAwait(false);
            }

            Console.WriteLine(
                $"Script creation: {scriptCreationsSucceeded} succeeded, {scriptCreationsFailed} failed"
            );

            // With proper AsyncLocal isolation, all Script creations should succeed.
            await Assert
                .That(scriptCreationsSucceeded)
                .IsEqualTo(iterations)
                .Because(
                    "Exception-throwing probe in one async flow should not affect Script creation in another flow"
                );
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
