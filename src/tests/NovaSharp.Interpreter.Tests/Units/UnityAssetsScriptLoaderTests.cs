namespace NovaSharp.Interpreter.Tests.Units
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
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    [PlatformDetectorIsolation]
    public sealed class UnityAssetsScriptLoaderTests
    {
        private static readonly string[] ExpectedLoadedScripts = { "alpha.lua", "beta.lua" };
        private static readonly string[] ReflectionLoadedScripts =
        {
            "from_unity.lua",
            "extra.lua",
        };

        private static IEnumerable<TestCaseData> RecoverableExceptionCases()
        {
            yield return CreateRecoverableCase(typeof(ScriptRuntimeException));
            yield return CreateRecoverableCase(typeof(FileNotFoundException));
            yield return CreateRecoverableCase(typeof(DirectoryNotFoundException));
            yield return CreateRecoverableCase(typeof(UnauthorizedAccessException));
            yield return CreateRecoverableCase(typeof(IOException));
            yield return CreateRecoverableCase(typeof(SecurityException));
            yield return CreateRecoverableCase(typeof(InvalidOperationException));
            yield return CreateRecoverableCase(typeof(NotSupportedException));
            yield return CreateRecoverableCase(typeof(TypeLoadException));
            yield return CreateRecoverableCase(typeof(MissingMethodException));
            yield return CreateRecoverableCase(typeof(TargetInvocationException));
            yield return CreateRecoverableCase(typeof(ArgumentException));
        }

        private static TestCaseData CreateRecoverableCase(Type exceptionType)
        {
            string testName = string.Format(
                CultureInfo.InvariantCulture,
                "ReflectionConstructorHandlesRecoverableExceptions({0})",
                exceptionType.Name
            );
            return new TestCaseData(exceptionType).SetName(testName);
        }

        [TearDown]
        public void CleanupUnityHarness()
        {
            UnityEngineReflectionHarness.Reset();
        }

        [Test]
        public void LoadFileReturnsResourceContentRegardlessOfPath()
        {
            Dictionary<string, string> resources = new(StringComparer.OrdinalIgnoreCase)
            {
                ["init.lua"] = "print('hi')",
            };

            UnityAssetsScriptLoader loader = new(resources);

            object script = loader.LoadFile("scripts/init.lua", null);

            Assert.That(script, Is.EqualTo("print('hi')"));
        }

        [Test]
        public void LoadFileThrowsHelpfulMessageWhenMissing()
        {
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() =>
                loader.LoadFile("missing.lua", null)
            );
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain(UnityAssetsScriptLoader.DefaultPath));
        }

        [Test]
        public void ScriptFileExistsHandlesPathsAndExtensions()
        {
            UnityAssetsScriptLoader loader = new(
                new Dictionary<string, string> { ["secondary.lua"] = "" }
            );

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("secondary.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("Scripts/secondary.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("Scripts/other.lua"), Is.False);
            });
        }

        [Test]
        public void GetLoadedScriptsReturnsSnapshotOfKeys()
        {
            Dictionary<string, string> resources = new() { ["alpha.lua"] = "", ["beta.lua"] = "" };

            UnityAssetsScriptLoader loader = new(resources);

            string[] loaded = loader.GetLoadedScripts();

            Assert.That(loaded, Is.EquivalentTo(ExpectedLoadedScripts));
        }

        [Test]
        public void DefaultConstructorLoadsScriptsViaReflection()
        {
            Dictionary<string, string> resources = new()
            {
                ["from_unity.lua"] = "print('unity')",
                ["extra.lua"] = "-- stub",
            };

            UnityEngineReflectionHarness.EnsureUnityAssemblies(resources);

            UnityAssetsScriptLoader loader = new("Custom/Unity/Scripts");

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("from_unity.lua"), Is.True);
                Assert.That(
                    loader.LoadFile("Custom/Unity/Scripts/from_unity.lua", null),
                    Is.EqualTo("print('unity')")
                );
                Assert.That(loader.GetLoadedScripts(), Is.EquivalentTo(ReflectionLoadedScripts));
                Assert.That(
                    UnityEngineReflectionHarness.LastRequestedPath,
                    Is.EqualTo("Custom/Unity/Scripts")
                );
            });
        }

        [Test]
        public void DefaultConstructorUsesDefaultPath()
        {
            Dictionary<string, string> resources = new() { ["alpha.lua"] = "return 1" };

            UnityEngineReflectionHarness.EnsureUnityAssemblies(resources);
            UnityEngineReflectionHarness.SetThrowOnLoad(false);

            UnityAssetsScriptLoader loader = new();

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("alpha.lua"), Is.True);
                Assert.That(
                    UnityEngineReflectionHarness.LastRequestedPath,
                    Is.EqualTo(UnityAssetsScriptLoader.DefaultPath)
                );
                Assert.That(loader.LoadFile("alpha.lua", null), Is.EqualTo("return 1"));
            });
        }

        [Test]
        public void ReflectionConstructorHandlesFailuresGracefully()
        {
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
            UnityEngineReflectionHarness.SetThrowOnLoad(true);

            try
            {
                UnityAssetsScriptLoader loader = new("Broken/Path");
                Assert.That(loader.ScriptFileExists("missing.lua"), Is.False);
                Assert.That(loader.GetLoadedScripts(), Is.Empty);
            }
            finally
            {
                UnityEngineReflectionHarness.SetThrowOnLoad(false);
            }
        }

        [TestCaseSource(nameof(RecoverableExceptionCases))]
        public void ReflectionConstructorHandlesRecoverableExceptions(System.Type exceptionType)
        {
            ArgumentNullException.ThrowIfNull(exceptionType);
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
            string diagnostic = string.Format(
                CultureInfo.InvariantCulture,
                "Simulating Unity load failure with exception type {0}",
                exceptionType.FullName
            );
            TestContext.WriteLine(diagnostic);
            UnityEngineReflectionHarness.SetThrowOnLoad(() =>
            {
                return CreateExceptionInstance(exceptionType);
            });

            try
            {
                UnityAssetsScriptLoader loader = new("Fallback/Scripts");
                Assert.Multiple(() =>
                {
                    Assert.That(loader.ScriptFileExists("any.lua"), Is.False);
                    Assert.That(loader.GetLoadedScripts(), Is.Empty);
                });
            }
            finally
            {
                UnityEngineReflectionHarness.SetThrowOnLoad(false);
            }
        }

        [Test]
        public void ReflectionConstructorUnwrapsTargetInvocationExceptions()
        {
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
            UnityEngineReflectionHarness.SetThrowOnLoad(() =>
                new TargetInvocationException(new SecurityException("denied"))
            );

            try
            {
                UnityAssetsScriptLoader loader = new("Secure/Scripts");

                Assert.Multiple(() =>
                {
                    Assert.That(loader.GetLoadedScripts(), Is.Empty);
                    Assert.That(
                        UnityEngineReflectionHarness.LastRequestedPath,
                        Is.EqualTo("Secure/Scripts")
                    );
                });
            }
            finally
            {
                UnityEngineReflectionHarness.SetThrowOnLoad(false);
            }
        }

        [Test]
        public void ReflectionConstructorPropagatesNonRecoverableExceptions()
        {
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
            UnityEngineReflectionHarness.SetThrowOnLoad(() =>
                new UnityHarnessFatalException("fatal")
            );

            try
            {
                Assert.That(
                    () => new UnityAssetsScriptLoader("Fatal/Scripts"),
                    Throws.TypeOf<UnityHarnessFatalException>()
                );
            }
            finally
            {
                UnityEngineReflectionHarness.SetThrowOnLoad(false);
            }
        }

        private static Exception CreateExceptionInstance(System.Type exceptionType)
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

        [Test]
        public void LoadFileThrowsArgumentNullWhenNameMissing()
        {
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            Assert.That(
                () => loader.LoadFile(null, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("file")
            );
        }

        [Test]
        public void ScriptFileExistsThrowsArgumentNullWhenNameMissing()
        {
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            Assert.That(
                () => loader.ScriptFileExists(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("name")
            );
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

        internal static void Reset()
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
            PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(false);
            PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(false);
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
    }
}
