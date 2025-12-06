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
            using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() => true);

            bool initialProbe = PlatformAutoDetector.IsRunningOnAot;
            await Assert.That(initialProbe).IsTrue();

            probe.Dispose();

            bool cachedValue = PlatformAutoDetector.IsRunningOnAot;
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
            IReadOnlyList<Type> exceptionTypes = new[]
            {
                typeof(PlatformNotSupportedException),
                typeof(MemberAccessException),
                typeof(NotSupportedException),
                typeof(InvalidOperationException),
                typeof(TypeLoadException),
                typeof(SecurityException),
            };

            foreach (Type exceptionType in exceptionTypes)
            {
                using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
                using IDisposable probe = PlatformDetectorScope.OverrideAotProbe(() =>
                {
                    Exception instance = (Exception)Activator.CreateInstance(exceptionType);
                    throw instance;
                });

                bool isRunningOnAot = PlatformAutoDetector.IsRunningOnAot;
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
