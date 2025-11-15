namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class UnityAssetsScriptLoaderTests
    {
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

            Exception ex = Assert.Throws<Exception>(() => loader.LoadFile("missing.lua", null!));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain(UnityAssetsScriptLoader.DefaultPath));
        }

        [Test]
        public void ScriptFileExistsHandlesPathsAndExtensions()
        {
            UnityAssetsScriptLoader loader = new(
                new Dictionary<string, string>
                {
                    ["secondary.lua"] = "",
                }
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
            Dictionary<string, string> resources = new()
            {
                ["alpha.lua"] = "",
                ["beta.lua"] = "",
            };

            UnityAssetsScriptLoader loader = new(resources);

            string[] loaded = loader.GetLoadedScripts();

            Assert.That(loaded, Is.EquivalentTo(new[] { "alpha.lua", "beta.lua" }));
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
                Assert.That(loader.LoadFile("Custom/Unity/Scripts/from_unity.lua", null!), Is.EqualTo("print('unity')"));
                Assert.That(loader.GetLoadedScripts(), Is.EquivalentTo(new[] { "from_unity.lua", "extra.lua" }));
                Assert.That(UnityEngineReflectionHarness.LastRequestedPath, Is.EqualTo("Custom/Unity/Scripts"));
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

        public static class UnityEngineReflectionHarness
        {
            private static readonly object SyncRoot = new();
            private static bool _assemblyBuilt;
            private static bool _throwOnLoad;
            private static Assembly _unityAssembly;
            private static Dictionary<string, string> _scripts = new(StringComparer.OrdinalIgnoreCase);

            internal static string LastRequestedPath { get; private set; } = string.Empty;

            internal static void EnsureUnityAssemblies(Dictionary<string, string> scripts)
            {
                lock (SyncRoot)
                {
                    _scripts = new Dictionary<string, string>(scripts, StringComparer.OrdinalIgnoreCase);
                    if (!_assemblyBuilt)
                    {
                        BuildUnityAssembly();
                        _assemblyBuilt = true;
                        EnsureTypeIsAvailable("UnityEngine.Resources, UnityEngine");
                        EnsureTypeIsAvailable("UnityEngine.TextAsset, UnityEngine");
                    }
                }
            }

            internal static void SetThrowOnLoad(bool shouldThrow)
            {
                _throwOnLoad = shouldThrow;
            }

            public static Array BuildAssetArray(string assetsPath, Type textAssetType)
            {
                LastRequestedPath = assetsPath;

                if (_throwOnLoad)
                {
                    throw new InvalidOperationException("Simulated Unity failure.");
                }

                Array array = Array.CreateInstance(textAssetType, _scripts.Count);
                int index = 0;

                foreach (KeyValuePair<string, string> script in _scripts)
                {
                    object instance = Activator.CreateInstance(
                        textAssetType,
                        BindingFlags.Public | BindingFlags.Instance,
                        binder: null,
                        args: new object[] { script.Key, script.Value },
                        culture: null
                    );

                    array.SetValue(instance, index++);
                }

                return array;
            }

            private static void BuildUnityAssembly()
            {
                AssemblyName name = new("UnityEngine");
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
                _unityAssembly = assembly;
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

                FieldBuilder nameField = builder.DefineField("_name", typeof(string), FieldAttributes.Private);
                FieldBuilder textField = builder.DefineField("_text", typeof(string), FieldAttributes.Private);

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
                    TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract
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
                    nameof(BuildAssetArray),
                    BindingFlags.Public | BindingFlags.Static
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
                if (_unityAssembly != null && args.Name.StartsWith("UnityEngine", StringComparison.Ordinal))
                {
                    return _unityAssembly;
                }

                return null;
            }
        }
    }
}
