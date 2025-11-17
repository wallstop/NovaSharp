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
        public void GetDefaultPlatform_ReturnsLimitedAccessorWhenUnityFlagged()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.OverrideFlags(unity: true);

            IPlatformAccessor platform = PlatformAutoDetector.GetDefaultPlatform();

            Assert.That(platform, Is.TypeOf<LimitedPlatformAccessor>());
        }

        [Test]
        public void GetDefaultScriptLoader_DetectsUnityAssemblies()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();
            UnityAssemblyProbe.EnsureLoaded();

            IScriptLoader loader = PlatformAutoDetector.GetDefaultScriptLoader();

            Assert.That(loader, Is.TypeOf<UnityAssetsScriptLoader>());
            Assert.That(PlatformAutoDetector.IsRunningOnUnity, Is.True);
        }

        [Test]
        public void IsRunningOnAot_UsesCachedValueAfterProbe()
        {
            using PlatformDetectorScope scope = PlatformDetectorScope.ResetForDetection();

            bool initialProbe = PlatformAutoDetector.IsRunningOnAot;
            Assert.That(initialProbe, Is.False);

            PlatformDetectorScope.SetAotValue(true);

            bool cached = PlatformAutoDetector.IsRunningOnAot;

            Assert.That(cached, Is.True);
        }

        private sealed class PlatformDetectorScope : IDisposable
        {
            private static readonly string[] FlagPropertyNames =
            {
                "IsRunningOnUnity",
                "IsUnityNative",
                "IsRunningOnMono",
                "IsPortableFramework",
                "IsRunningOnClr4",
                "IsUnityIl2Cpp",
            };

            private readonly Dictionary<PropertyInfo, object> originalFlags = new();
            private readonly bool? originalAot;
            private readonly bool originalAutoDetectionsDone;

            private PlatformDetectorScope()
            {
                foreach (string name in FlagPropertyNames)
                {
                    PropertyInfo property = GetProperty(name);
                    originalFlags[property] = property.GetValue(null, null)!;
                }

                originalAot = (bool?)GetField("_isRunningOnAot").GetValue(null);
                originalAutoDetectionsDone = (bool)GetField("_autoDetectionsDone").GetValue(null);
            }

            public static PlatformDetectorScope ResetForDetection()
            {
                PlatformDetectorScope scope = new();
                foreach (PropertyInfo property in scope.originalFlags.Keys)
                {
                    property.SetValue(null, false);
                }

                SetAotValue(null);
                SetAutoDetectionsDone(false);
                return scope;
            }

            public static PlatformDetectorScope OverrideFlags(bool unity)
            {
                PlatformDetectorScope scope = ResetForDetection();
                GetProperty("IsRunningOnUnity").SetValue(null, unity);
                SetAutoDetectionsDone(true);
                return scope;
            }

            public static void SetAotValue(bool? value)
            {
                GetField("_isRunningOnAot").SetValue(null, value);
            }

            public void Dispose()
            {
                foreach ((PropertyInfo property, object value) in originalFlags)
                {
                    property.SetValue(null, value);
                }

                SetAotValue(originalAot);
                SetAutoDetectionsDone(originalAutoDetectionsDone);
            }

            private static void SetAutoDetectionsDone(bool value)
            {
                GetField("_autoDetectionsDone").SetValue(null, value);
            }

            private static PropertyInfo GetProperty(string name)
            {
                return typeof(PlatformAutoDetector).GetProperty(
                    name,
                    BindingFlags.Public | BindingFlags.Static
                )!;
            }

            private static FieldInfo GetField(string name)
            {
                return typeof(PlatformAutoDetector).GetField(
                    name,
                    BindingFlags.NonPublic | BindingFlags.Static
                )!;
            }
        }

        private static class UnityAssemblyProbe
        {
            private static bool loaded;

            public static void EnsureLoaded()
            {
                if (loaded)
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

                loaded = true;
            }
        }
    }
}
