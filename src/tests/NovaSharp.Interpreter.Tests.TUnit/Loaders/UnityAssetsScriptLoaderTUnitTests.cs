namespace NovaSharp.Interpreter.Tests.TUnit.Loaders
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    public sealed class UnityAssetsScriptLoaderTUnitTests
    {
        private static readonly string[] ExpectedLoadedScripts = { "alpha.lua", "beta.lua" };
        private static readonly string[] ReflectionLoadedScripts =
        {
            "from_unity.lua",
            "extra.lua",
        };

        private static readonly Type[] RecoverableExceptionTypes =
        {
            typeof(ScriptRuntimeException),
            typeof(FileNotFoundException),
            typeof(DirectoryNotFoundException),
            typeof(UnauthorizedAccessException),
            typeof(IOException),
            typeof(SecurityException),
            typeof(InvalidOperationException),
            typeof(NotSupportedException),
            typeof(TypeLoadException),
            typeof(MissingMethodException),
            typeof(TargetInvocationException),
            typeof(ArgumentException),
        };

        [global::TUnit.Core.Test]
        public async Task LoadFileReturnsResourceContentRegardlessOfPath()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            Dictionary<string, string> resources = new(StringComparer.OrdinalIgnoreCase)
            {
                ["init.lua"] = "print('hi')",
            };

            UnityAssetsScriptLoader loader = new(resources);

            object script = loader.LoadFile("scripts/init.lua", null);

            await Assert.That(script).IsEqualTo("print('hi')");
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileThrowsHelpfulMessageWhenMissing()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() =>
                loader.LoadFile("missing.lua", null)
            );
            await Assert.That(ex).IsNotNull();
            await Assert.That(ex.Message).Contains(UnityAssetsScriptLoader.DefaultPath);
        }

        [global::TUnit.Core.Test]
        public async Task ScriptFileExistsHandlesPathsAndExtensions()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityAssetsScriptLoader loader = new(
                new Dictionary<string, string> { ["secondary.lua"] = "" }
            );

            await Assert.That(loader.ScriptFileExists("secondary.lua")).IsTrue();
            await Assert.That(loader.ScriptFileExists("Scripts/secondary.lua")).IsTrue();
            await Assert.That(loader.ScriptFileExists("Scripts/other.lua")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task GetLoadedScriptsReturnsSnapshotOfKeys()
        {
            Dictionary<string, string> resources = new() { ["alpha.lua"] = "", ["beta.lua"] = "" };

            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityAssetsScriptLoader loader = new(resources);

            string[] loaded = loader.GetLoadedScripts();

            await Assert.That(loaded).IsEquivalentTo(ExpectedLoadedScripts);
        }

        [global::TUnit.Core.Test]
        public async Task DefaultConstructorLoadsScriptsViaReflection()
        {
            Dictionary<string, string> resources = new()
            {
                ["from_unity.lua"] = "print('unity')",
                ["extra.lua"] = "-- stub",
            };

            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityEngineReflectionHarness.EnsureUnityAssemblies(resources);
            UnityAssetsScriptLoader loader = new("Custom/Unity/Scripts");

            await Assert.That(loader.ScriptFileExists("from_unity.lua")).IsTrue();
            await Assert
                .That(loader.LoadFile("Custom/Unity/Scripts/from_unity.lua", null))
                .IsEqualTo("print('unity')");
            await Assert.That(loader.GetLoadedScripts()).IsEquivalentTo(ReflectionLoadedScripts);
            await Assert
                .That(UnityEngineReflectionHarness.LastRequestedPath)
                .IsEqualTo("Custom/Unity/Scripts");
        }

        [global::TUnit.Core.Test]
        public async Task DefaultConstructorUsesDefaultPath()
        {
            Dictionary<string, string> resources = new() { ["alpha.lua"] = "return 1" };

            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityEngineReflectionHarness.EnsureUnityAssemblies(resources);
            UnityEngineReflectionHarness.SetThrowOnLoad(false);
            UnityAssetsScriptLoader loader = new();

            await Assert.That(loader.ScriptFileExists("alpha.lua")).IsTrue();
            await Assert
                .That(UnityEngineReflectionHarness.LastRequestedPath)
                .IsEqualTo(UnityAssetsScriptLoader.DefaultPath);
            await Assert.That(loader.LoadFile("alpha.lua", null)).IsEqualTo("return 1");
        }

        [global::TUnit.Core.Test]
        public async Task ReflectionConstructorHandlesFailuresGracefully()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
            UnityEngineReflectionHarness.SetThrowOnLoad(true);
            UnityAssetsScriptLoader loader = new("Broken/Path");
            await Assert.That(loader.ScriptFileExists("missing.lua")).IsFalse();
            await Assert.That(loader.GetLoadedScripts()).IsEmpty();
        }

        [global::TUnit.Core.Test]
        public async Task ReflectionConstructorHandlesRecoverableExceptions()
        {
            foreach (Type exceptionType in RecoverableExceptionTypes)
            {
                using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
                UnityEngineReflectionHarness.EnsureUnityAssemblies(
                    new Dictionary<string, string>()
                );
                string diagnostic = string.Format(
                    CultureInfo.InvariantCulture,
                    "Simulating Unity load failure with exception type {0}",
                    exceptionType.FullName
                );
                Console.WriteLine(diagnostic);
                UnityEngineReflectionHarness.SetThrowOnLoad(() =>
                {
                    return CreateExceptionInstance(exceptionType);
                });

                UnityAssetsScriptLoader loader = new("Fallback/Scripts");
                await Assert.That(loader.ScriptFileExists("any.lua")).IsFalse();
                await Assert.That(loader.GetLoadedScripts()).IsEmpty();
            }
        }

        [global::TUnit.Core.Test]
        public async Task ReflectionConstructorUnwrapsTargetInvocationExceptions()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
            UnityEngineReflectionHarness.SetThrowOnLoad(() =>
                new TargetInvocationException(new SecurityException("denied"))
            );
            UnityAssetsScriptLoader loader = new("Secure/Scripts");

            await Assert.That(loader.GetLoadedScripts()).IsEmpty();
            await Assert
                .That(UnityEngineReflectionHarness.LastRequestedPath)
                .IsEqualTo("Secure/Scripts");
        }

        [global::TUnit.Core.Test]
        public async Task ReflectionConstructorPropagatesNonRecoverableExceptions()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
            UnityEngineReflectionHarness.SetThrowOnLoad(() =>
                new UnityHarnessFatalException("fatal")
            );
            UnityHarnessFatalException exception = Assert.Throws<UnityHarnessFatalException>(() =>
            {
                _ = new UnityAssetsScriptLoader("Fatal/Scripts");
            });
            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileThrowsArgumentNullWhenNameMissing()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.LoadFile(null, null);
            });

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception.ParamName).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        public async Task ScriptFileExistsThrowsArgumentNullWhenNameMissing()
        {
            using IDisposable harnessScope = UnityEngineReflectionHarness.Reset();
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ScriptFileExists(null);
            });

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception.ParamName).IsEqualTo("name");
        }

        private static Exception CreateExceptionInstance(Type exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);

            ConstructorInfo ctor = exceptionType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null
            );

            if (ctor != null)
            {
                return (Exception)ctor.Invoke(null);
            }

            Exception constructed = TryInvokeConstructorWithDefaults(exceptionType);
            if (constructed != null)
            {
                return constructed;
            }

            return InstantiateWithoutConstructor(exceptionType);
        }

        private static Exception TryInvokeConstructorWithDefaults(Type exceptionType)
        {
            ConstructorInfo[] constructors = exceptionType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );

            foreach (ConstructorInfo candidate in constructors)
            {
                ParameterInfo[] parameters = candidate.GetParameters();
                object[] arguments = new object[parameters.Length];
                bool canUse = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (
                        !TryGetDefaultArgument(
                            exceptionType,
                            parameters[i].ParameterType,
                            out object argument
                        )
                    )
                    {
                        canUse = false;
                        break;
                    }

                    arguments[i] = argument;
                }

                if (!canUse)
                {
                    continue;
                }

                return (Exception)candidate.Invoke(arguments);
            }

            return null;
        }

        private static bool TryGetDefaultArgument(
            Type declaringType,
            Type parameterType,
            out object argument
        )
        {
            if (parameterType == typeof(string))
            {
                argument = string.Empty;
                return true;
            }

            if (typeof(Exception).IsAssignableFrom(parameterType))
            {
                argument = new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Simulated inner exception for {0}.",
                        declaringType.FullName
                    )
                );
                return true;
            }

            if (typeof(SerializationInfo).IsAssignableFrom(parameterType))
            {
                argument = null;
                return false;
            }

            if (parameterType == typeof(StreamingContext))
            {
                argument = default(StreamingContext);
                return true;
            }

            if (parameterType.IsValueType)
            {
                argument = Activator.CreateInstance(parameterType);
                return true;
            }

            argument = null;
            return true;
        }

        private static Exception InstantiateWithoutConstructor(Type exceptionType)
        {
            object instance = RuntimeHelpers.GetUninitializedObject(exceptionType);
            return (Exception)instance;
        }

        private sealed class UnityHarnessFatalException : Exception
        {
            public UnityHarnessFatalException() { }

            public UnityHarnessFatalException(string message)
                : base(message) { }

            public UnityHarnessFatalException(string message, Exception innerException)
                : base(message, innerException) { }
        }
    }

    public static class UnityEngineReflectionHarness
    {
        private static readonly object SyncRoot = new();
        private static bool AssemblyBuilt;
        private static Func<Exception> ThrowOnLoadFactory;
        private static Assembly UnityAssembly;
        private static ResolveEventHandler ResolveHandler;
        private static Dictionary<string, string> Scripts = new(StringComparer.OrdinalIgnoreCase);

        internal static string LastRequestedPath { get; private set; } = string.Empty;

        internal static void EnsureUnityAssemblies(Dictionary<string, string> scripts)
        {
            lock (SyncRoot)
            {
                Scripts = new Dictionary<string, string>(scripts, StringComparer.OrdinalIgnoreCase);
                if (!AssemblyBuilt)
                {
                    BuildUnityAssembly();
                    AssemblyBuilt = true;
                    EnsureTypeIsAvailable("UnityEngine.Resources, UnityEngine");
                    EnsureTypeIsAvailable("UnityEngine.TextAsset, UnityEngine");
                }
            }
        }

        internal static void SetThrowOnLoad(bool shouldThrow)
        {
            ThrowOnLoadFactory = shouldThrow
                ? () => new InvalidOperationException("Simulated Unity failure.")
                : null;
        }

        internal static void SetThrowOnLoad(Func<Exception> exceptionFactory)
        {
            ThrowOnLoadFactory = exceptionFactory;
        }

        internal static IDisposable Reset()
        {
            ResetState();
            PlatformDetectorOverrideScope platformScope =
                PlatformDetectorOverrideScope.ForceFileSystemLoader();
            return new ResetScope(platformScope);
        }

        private static void ResetState()
        {
            lock (SyncRoot)
            {
                Scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ThrowOnLoadFactory = null;
                LastRequestedPath = string.Empty;
                AssemblyBuilt = false;
                UnityAssembly = null;
                if (ResolveHandler != null)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= ResolveHandler;
                    ResolveHandler = null;
                }
            }
        }

        public static Array BuildAssetArray(string assetsPath, Type textAssetType)
        {
            LastRequestedPath = assetsPath;

            Func<Exception> exceptionFactory = ThrowOnLoadFactory;
            if (exceptionFactory != null)
            {
                throw exceptionFactory();
            }

            Array array = Array.CreateInstance(textAssetType, Scripts.Count);
            int index = 0;

            foreach (KeyValuePair<string, string> script in Scripts)
            {
                object instance = Activator.CreateInstance(textAssetType, script.Key, script.Value);

                array.SetValue(instance, index++);
            }

            return array;
        }

        private static void BuildUnityAssembly()
        {
            AssemblyName name = new("UnityEngine");
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                name,
                AssemblyBuilderAccess.Run
            );
            UnityAssembly = assembly;
            ResolveHandler = ResolveUnityAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveHandler;
            ModuleBuilder module = assembly.DefineDynamicModule("UnityEngine.Dynamic");

            CreateTextAssetType(module);
            CreateResourcesType(module);
        }

        private static void CreateTextAssetType(ModuleBuilder module)
        {
            TypeBuilder builder = module.DefineType(
                "UnityEngine.TextAsset",
                TypeAttributes.Public | TypeAttributes.Class
            );

            FieldBuilder nameField = builder.DefineField(
                "_name",
                typeof(string),
                FieldAttributes.Private
            );
            FieldBuilder textField = builder.DefineField(
                "_text",
                typeof(string),
                FieldAttributes.Private
            );

            ConstructorBuilder ctor = builder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new[] { typeof(string), typeof(string) }
            );

            ILGenerator il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, nameField);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, textField);
            il.Emit(OpCodes.Ret);

            CreateAutoGetter(builder, "name", nameField);
            CreateAutoGetter(builder, "text", textField);

            builder.CreateTypeInfo();
        }

        private static void CreateAutoGetter(
            TypeBuilder typeBuilder,
            string propertyName,
            FieldBuilder backingField
        )
        {
            PropertyBuilder property = typeBuilder.DefineProperty(
                propertyName,
                PropertyAttributes.None,
                typeof(string),
                null
            );

            MethodBuilder getter = typeBuilder.DefineMethod(
                $"get_{propertyName}",
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                typeof(string),
                Type.EmptyTypes
            );

            ILGenerator il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, backingField);
            il.Emit(OpCodes.Ret);

            property.SetGetMethod(getter);
        }

        private static void CreateResourcesType(ModuleBuilder module)
        {
            TypeBuilder builder = module.DefineType(
                "UnityEngine.Resources",
                TypeAttributes.Public
                    | TypeAttributes.Class
                    | TypeAttributes.Sealed
                    | TypeAttributes.Abstract
            );

            MethodBuilder loadAll = builder.DefineMethod(
                "LoadAll",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(Array),
                new[] { typeof(string), typeof(Type) }
            );

            ILGenerator il = loadAll.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            MethodInfo helper = typeof(UnityEngineReflectionHarness).GetMethod(
                nameof(BuildAssetArray)
            );
            il.Emit(OpCodes.Call, helper);
            il.Emit(OpCodes.Ret);

            builder.CreateTypeInfo();
        }

        private static void EnsureTypeIsAvailable(string qualifiedName)
        {
            Type type = Type.GetType(qualifiedName, throwOnError: false);
            if (type == null)
            {
                throw new InvalidOperationException($"Failed to load stub type '{qualifiedName}'.");
            }
        }

        private static Assembly ResolveUnityAssembly(object sender, ResolveEventArgs args)
        {
            if (
                UnityAssembly != null
                && args.Name.StartsWith("UnityEngine", StringComparison.Ordinal)
            )
            {
                return UnityAssembly;
            }

            return null;
        }

        private sealed class ResetScope : IDisposable
        {
            private readonly PlatformDetectorOverrideScope _platformScope;
            private bool _disposed;

            internal ResetScope(PlatformDetectorOverrideScope platformScope)
            {
                _platformScope = platformScope;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _platformScope.Dispose();
                ResetState();
                _disposed = true;
            }
        }
    }
}
