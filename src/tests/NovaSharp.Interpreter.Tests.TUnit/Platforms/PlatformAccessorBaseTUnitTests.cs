namespace NovaSharp.Interpreter.Tests.TUnit.Platforms
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NovaSharp.Interpreter.Tests;

    [PlatformDetectorIsolation]
    public sealed class PlatformAccessorBaseTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GetPlatformNameIncludesMonoClr2Suffix()
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

            await Assert.That(name).StartsWith("testprefix.");
            await Assert.That(name).Contains(".mono");
            await Assert.That(name).Contains(".clr2");
            await Assert.That(name).DoesNotContain(".portable");
            await Assert.That(name).DoesNotContain(".aot");
        }

        [global::TUnit.Core.Test]
        public async Task GetPlatformNameIncludesUnityNativeMetadata()
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

            await Assert.That(name).Contains("unityprefix.unity.unknownhw.unknown");
            await Assert.That(name).Contains(".portable");
            await Assert.That(name).Contains(".clr4");
            await Assert.That(name).Contains(".aot");
        }

        [global::TUnit.Core.Test]
        public async Task GetPlatformNameUsesUnityDllMonoWhenNotNative()
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

            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsTrue();
            await Assert.That(PlatformAutoDetector.IsUnityNative).IsFalse();
            await Assert.That(PlatformAutoDetector.IsRunningOnMono).IsTrue();
            await Assert.That(name).Contains("mono-unity.unity.dll.mono");
        }

        [global::TUnit.Core.Test]
        public async Task GetPlatformNameUsesUnityDllUnknownWhenNotMono()
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

            await Assert.That(PlatformAutoDetector.IsRunningOnUnity).IsTrue();
            await Assert.That(PlatformAutoDetector.IsUnityNative).IsFalse();
            await Assert.That(PlatformAutoDetector.IsRunningOnMono).IsFalse();
            await Assert.That(name).Contains("unknown-unity.unity.dll.unknown");
        }

        [global::TUnit.Core.Test]
        public async Task GetPlatformNameFallsBackToDotnetWhenNotUnityOrMono()
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

            await Assert.That(name).Contains(".dotnet");
        }

        [global::TUnit.Core.Test]
        public async Task DefaultInputWithPromptCallsObsoleteOverload()
        {
            TestPlatformAccessor accessor = new("input");
            accessor.DefaultInputResult = "line";

            string result = accessor.DefaultInput("prompt> ");

            await Assert.That(accessor.ObsoleteDefaultInputInvocations).IsEqualTo(1);
            await Assert.That(result).IsEqualTo("line");
        }

        [global::TUnit.Core.Test]
        public async Task DefaultInputReturnsNullForBaseImplementation()
        {
            BaseDefaultInputAccessor accessor = new("base");

            string result = accessor.DefaultInput("prompt> ");

            await Assert.That(result).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task IsRunningOnAotReflectsDetectorFlag()
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
            await Assert.That(accessor.IsRunningOnAOT()).IsTrue();
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
                PlatformAutoDetector.TestHooks.SetUnityDetectionOverride(null);
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
