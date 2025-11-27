namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UnityAssetsScriptLoaderTests
    {
        private static readonly string[] ExpectedLoadedScripts = { "alpha.lua", "beta.lua" };
        private static readonly string[] ReflectionLoadedScripts =
        {
            "from_unity.lua",
            "extra.lua",
        };
        private static readonly System.Type[] RecoverableExceptionTypes =
        {
            typeof(ScriptRuntimeException),
            typeof(FileNotFoundException),
            typeof(DirectoryNotFoundException),
            typeof(UnauthorizedAccessException),
            typeof(IOException),
            typeof(System.Security.SecurityException),
            typeof(InvalidOperationException),
            typeof(NotSupportedException),
            typeof(TypeLoadException),
            typeof(MissingMethodException),
            typeof(TargetInvocationException),
            typeof(ArgumentException),
        };

        [Test]
        public void LoadFileReturnsResourceContentRegardlessOfPath()
        {
            Dictionary<string, string> resources = new(StringComparer.OrdinalIgnoreCase)
            {
                ["init.lua"] = "print('hi')",
            };

            UnityAssetsScriptLoader loader = new(resources);

            object script = loader.LoadFile("scripts/init.lua", null!);

            Assert.That(script, Is.EqualTo("print('hi')"));
        }

        [Test]
        public void LoadFileThrowsHelpfulMessageWhenMissing()
        {
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() =>
                loader.LoadFile("missing.lua", null!)
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
                    loader.LoadFile("Custom/Unity/Scripts/from_unity.lua", null!),
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

        [TestCaseSource(nameof(RecoverableExceptionTypes))]
        public void ReflectionConstructorHandlesRecoverableExceptions(System.Type exceptionType)
        {
            UnityEngineReflectionHarness.EnsureUnityAssemblies(new Dictionary<string, string>());
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

        private static Exception CreateExceptionInstance(System.Type exceptionType)
        {
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

            return (Exception)Activator.CreateInstance(exceptionType)!;
        }

        [Test]
        public void LoadFileThrowsArgumentNullWhenNameMissing()
        {
            UnityAssetsScriptLoader loader = new(new Dictionary<string, string>());

            Assert.That(
                () => loader.LoadFile(null, null!),
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
    }

    public static class UnityEngineReflectionHarness
    {
        private static readonly object SyncRoot = new();
        private static bool AssemblyBuilt;
        private static Func<Exception> ThrowOnLoadFactory;
        private static Assembly UnityAssembly;
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
            AppDomain.CurrentDomain.AssemblyResolve += ResolveUnityAssembly;
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
