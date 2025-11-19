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
        public void IsRunningOnAotUsesCachedValueAfterProbe()
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

            private readonly Dictionary<PropertyInfo, object> _originalFlags = new();
            private readonly bool? _originalAot;
            private readonly bool _originalAutoDetectionsDone;

            private PlatformDetectorScope()
            {
                foreach (string name in FlagPropertyNames)
                {
                    PropertyInfo property = GetProperty(name);
                    _originalFlags[property] = property.GetValue(null, null)!;
                }

                _originalAot = (bool?)GetField("RunningOnAotCache").GetValue(null);
                _originalAutoDetectionsDone = (bool)GetField("AutoDetectionsDone").GetValue(null);
            }

            public static PlatformDetectorScope ResetForDetection()
            {
                PlatformDetectorScope scope = new();
                foreach (PropertyInfo property in scope._originalFlags.Keys)
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
                GetField("RunningOnAotCache").SetValue(null, value);
            }

            public void Dispose()
            {
                foreach ((PropertyInfo property, object value) in _originalFlags)
                {
                    property.SetValue(null, value);
                }

                SetAotValue(_originalAot);
                SetAutoDetectionsDone(_originalAutoDetectionsDone);
            }

            private static void SetAutoDetectionsDone(bool value)
            {
                GetField("AutoDetectionsDone").SetValue(null, value);
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
