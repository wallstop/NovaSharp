namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
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

            string result = accessor.DefaultInput("prompt> ");

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

            public override CoreModules FilterSupportedCoreModules(CoreModules coreModules)
            {
                return coreModules;
            }

            public override string GetEnvironmentVariable(string envvarname)
            {
                return Environment.GetEnvironmentVariable(envvarname);
            }
        }

        private sealed class BaseDefaultInputAccessor : PlatformAccessorBase
        {
            private readonly string _prefix;

            public BaseDefaultInputAccessor(string prefix)
            {
                _prefix = prefix;
            }

            public override string GetPlatformNamePrefix()
            {
                return _prefix;
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

            public override CoreModules FilterSupportedCoreModules(CoreModules coreModules)
            {
                return coreModules;
            }

            public override string GetEnvironmentVariable(string envvarname)
            {
                return Environment.GetEnvironmentVariable(envvarname);
            }
        }

        private sealed class PlatformFlagScope : IDisposable
        {
            private readonly PlatformAutoDetector.PlatformDetectorSnapshot _snapshot;

            private PlatformFlagScope()
            {
                _snapshot = PlatformAutoDetector.TestHooks.CaptureState();
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
                PlatformAutoDetector.TestHooks.SetFlags(
                    isRunningOnUnity: unity,
                    isUnityNative: unityNative,
                    isRunningOnMono: mono,
                    isPortableFramework: portable,
                    isRunningOnClr4: clr4
                );
                PlatformAutoDetector.TestHooks.SetRunningOnAot(aot);
                PlatformAutoDetector.TestHooks.SetAutoDetectionsDone(true);
                return scope;
            }

            public void Dispose()
            {
                PlatformAutoDetector.TestHooks.RestoreState(_snapshot);
            }
        }
    }
}
