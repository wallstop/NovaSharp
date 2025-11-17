namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PlatformAccessorBaseTests
    {
        [Test]
        public void GetPlatformNameIncludesMonoClr2Suffix()
        {
            using PlatformFlagScope scope = PlatformFlagScope.Override(
                unity: false,
                unityNative: false,
                mono: true,
                portable: false,
                clr4: false,
                aot: false
            );

            TestPlatformAccessor accessor = new("testprefix");
            string name = accessor.GetPlatformName();

            Assert.Multiple(() =>
            {
                Assert.That(name, Does.StartWith("testprefix."));
                Assert.That(name, Does.Contain(".mono"));
                Assert.That(name, Does.Contain(".clr2"));
                Assert.That(name, Does.Not.Contain(".portable"));
                Assert.That(name, Does.Not.Contain(".aot"));
            });
        }

        [Test]
        public void GetPlatformNameIncludesUnityNativeMetadata()
        {
            using PlatformFlagScope scope = PlatformFlagScope.Override(
                unity: true,
                unityNative: true,
                mono: false,
                portable: true,
                clr4: true,
                aot: true
            );

            TestPlatformAccessor accessor = new("unityprefix");
            string name = accessor.GetPlatformName();

            Assert.Multiple(() =>
            {
                Assert.That(name, Does.Contain("unityprefix.unity.unknownhw.unknown"));
                Assert.That(name, Does.Contain(".portable"));
                Assert.That(name, Does.Contain(".clr4"));
                Assert.That(name, Does.Contain(".aot"));
            });
        }

        [Test]
        public void GetPlatformNameUsesUnityDllMonoWhenNotNative()
        {
            using PlatformFlagScope scope = PlatformFlagScope.Override(
                unity: true,
                unityNative: false,
                mono: true,
                portable: false,
                clr4: true,
                aot: false
            );

            TestPlatformAccessor accessor = new("mono-unity");
            string name = accessor.GetPlatformName();

            Assert.Multiple(() =>
            {
                Assert.That(PlatformAutoDetector.IsRunningOnUnity, Is.True);
                Assert.That(PlatformAutoDetector.IsUnityNative, Is.False);
                Assert.That(PlatformAutoDetector.IsRunningOnMono, Is.True);
                Assert.That(name, Does.Contain("mono-unity.unity.dll.mono"));
            });
        }

        [Test]
        public void GetPlatformNameUsesUnityDllUnknownWhenNotMono()
        {
            using PlatformFlagScope scope = PlatformFlagScope.Override(
                unity: true,
                unityNative: false,
                mono: false,
                portable: false,
                clr4: true,
                aot: false
            );

            TestPlatformAccessor accessor = new("unknown-unity");
            string name = accessor.GetPlatformName();

            Assert.Multiple(() =>
            {
                Assert.That(PlatformAutoDetector.IsRunningOnUnity, Is.True);
                Assert.That(PlatformAutoDetector.IsUnityNative, Is.False);
                Assert.That(PlatformAutoDetector.IsRunningOnMono, Is.False);
                Assert.That(name, Does.Contain("unknown-unity.unity.dll.unknown"));
            });
        }

        [Test]
        public void GetPlatformNameFallsBackToDotnetWhenNotUnityOrMono()
        {
            using PlatformFlagScope scope = PlatformFlagScope.Override(
                unity: false,
                unityNative: false,
                mono: false,
                portable: false,
                clr4: true,
                aot: false
            );

            TestPlatformAccessor accessor = new("managed");
            string name = accessor.GetPlatformName();

            Assert.That(name, Does.Contain(".dotnet"));
        }

        [Test]
        public void DefaultInputWithPromptCallsObsoleteOverload()
        {
            TestPlatformAccessor accessor = new("input");
            accessor.DefaultInputResult = "line";

            string result = accessor.DefaultInput("prompt> ");

            Assert.Multiple(() =>
            {
                Assert.That(accessor.ObsoleteDefaultInputInvocations, Is.EqualTo(1));
                Assert.That(result, Is.EqualTo("line"));
            });
        }

        [Test]
        public void DefaultInputReturnsNullForBaseImplementation()
        {
            BaseDefaultInputAccessor accessor = new("base");

            string result = accessor.DefaultInput();

            Assert.That(result, Is.Null);
        }

        [Test]
        public void IsRunningOnAotReflectsDetectorFlag()
        {
            using PlatformFlagScope scope = PlatformFlagScope.Override(
                unity: false,
                unityNative: false,
                mono: false,
                portable: false,
                clr4: true,
                aot: true
            );

            TestPlatformAccessor accessor = new("aot");
            Assert.That(accessor.IsRunningOnAOT(), Is.True);
        }

        private sealed class TestPlatformAccessor : PlatformAccessorBase
        {
            private readonly string _prefix;

            public TestPlatformAccessor(string prefix)
            {
                _prefix = prefix;
            }

            public string DefaultInputResult { get; set; }

            public int ObsoleteDefaultInputInvocations { get; private set; }

            public override string GetPlatformNamePrefix()
            {
                return _prefix;
            }

            public override void DefaultPrint(string content)
            {
                // No-op for tests.
            }

            [Obsolete]
            public override string DefaultInput()
            {
                ObsoleteDefaultInputInvocations++;
                return DefaultInputResult;
            }

            public override Stream OpenFile(
                Script script,
                string filename,
                Encoding encoding,
                string mode
            )
            {
                return new MemoryStream();
            }

            public override Stream GetStandardStream(StandardFileType type)
            {
                return Stream.Null;
            }

            public override string GetTempFileName()
            {
                return Path.GetTempFileName();
            }

            public override void ExitFast(int exitCode)
            {
                // No-op in tests.
            }

            public override bool FileExists(string file)
            {
                return false;
            }

            public override void DeleteFile(string file) { }

            public override void MoveFile(string src, string dst) { }

            public override int ExecuteCommand(string cmdline)
            {
                return 0;
            }

            public override CoreModules FilterSupportedCoreModules(CoreModules module)
            {
                return module;
            }

            public override string GetEnvironmentVariable(string envvarname)
            {
                return Environment.GetEnvironmentVariable(envvarname);
            }
        }

        private sealed class BaseDefaultInputAccessor : PlatformAccessorBase
        {
            private readonly string prefix;

            public BaseDefaultInputAccessor(string prefix)
            {
                this.prefix = prefix;
            }

            public override string GetPlatformNamePrefix()
            {
                return prefix;
            }

            public override void DefaultPrint(string content) { }

            public override Stream OpenFile(
                Script script,
                string filename,
                Encoding encoding,
                string mode
            )
            {
                return Stream.Null;
            }

            public override Stream GetStandardStream(StandardFileType type)
            {
                return Stream.Null;
            }

            public override string GetTempFileName()
            {
                return Path.GetTempFileName();
            }

            public override void ExitFast(int exitCode) { }

            public override bool FileExists(string file)
            {
                return false;
            }

            public override void DeleteFile(string file) { }

            public override void MoveFile(string src, string dst) { }

            public override int ExecuteCommand(string cmdline)
            {
                return 0;
            }

            public override CoreModules FilterSupportedCoreModules(CoreModules module)
            {
                return module;
            }

            public override string GetEnvironmentVariable(string envvarname)
            {
                return Environment.GetEnvironmentVariable(envvarname);
            }
        }

        private sealed class PlatformFlagScope : IDisposable
        {
            private static readonly Dictionary<string, PropertyInfo> Properties = new()
            {
                { "Unity", GetProperty("IsRunningOnUnity") },
                { "UnityNative", GetProperty("IsUnityNative") },
                { "Mono", GetProperty("IsRunningOnMono") },
                { "Portable", GetProperty("IsPortableFramework") },
                { "Clr4", GetProperty("IsRunningOnClr4") },
            };

            private readonly Dictionary<string, object> _originalValues = new();
            private readonly bool? _originalAot;
            private readonly bool _originalAutoDetectionsDone;

            private PlatformFlagScope()
            {
                foreach ((string key, PropertyInfo property) in Properties)
                {
                    _originalValues[key] = property.GetValue(null, null);
                }

                FieldInfo aotField = GetField("_isRunningOnAot");
                _originalAot = (bool?)aotField.GetValue(null);

                FieldInfo autoField = GetField("_autoDetectionsDone");
                _originalAutoDetectionsDone = (bool)autoField.GetValue(null);
            }

            public static PlatformFlagScope Override(
                bool unity,
                bool unityNative,
                bool mono,
                bool portable,
                bool clr4,
                bool aot
            )
            {
                PlatformFlagScope scope = new();

                scope.SetProperty(Properties["Unity"], unity);
                scope.SetProperty(Properties["UnityNative"], unityNative);
                scope.SetProperty(Properties["Mono"], mono);
                scope.SetProperty(Properties["Portable"], portable);
                scope.SetProperty(Properties["Clr4"], clr4);

                FieldInfo aotField = GetField("_isRunningOnAot");
                aotField.SetValue(null, aot ? (bool?)true : (bool?)false);

                FieldInfo autoField = GetField("_autoDetectionsDone");
                autoField.SetValue(null, true);

                return scope;
            }

            public void Dispose()
            {
                foreach ((string key, object value) in _originalValues)
                {
                    SetProperty(Properties[key], value);
                }

                FieldInfo aotField = GetField("_isRunningOnAot");
                aotField.SetValue(null, _originalAot);

                FieldInfo autoField = GetField("_autoDetectionsDone");
                autoField.SetValue(null, _originalAutoDetectionsDone);
            }

            private static PropertyInfo GetProperty(string name)
            {
                return typeof(PlatformAutoDetector).GetProperty(
                    name,
                    BindingFlags.Static | BindingFlags.Public
                );
            }

            private static FieldInfo GetField(string name)
            {
                return typeof(PlatformAutoDetector).GetField(
                    name,
                    BindingFlags.Static | BindingFlags.NonPublic
                );
            }

            private static MethodInfo GetSetter(PropertyInfo property)
            {
                return property.GetSetMethod(true);
            }

            private void SetProperty(PropertyInfo property, object value)
            {
                MethodInfo setter = GetSetter(property);
                setter.Invoke(null, new[] { value });
            }
        }
    }
}
