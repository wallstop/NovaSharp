namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib.IO;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [ScriptGlobalOptionsIsolation]
    public sealed class IoStdHandleUserDataTUnitTests
    {
        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            return new Script(version, CoreModulePresets.Complete);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdInIsFileUserDataHandle(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue stdin = script.DoString("return io.stdin");

            await Assert.That(stdin.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert
                .That(stdin.UserData.Object)
                .IsTypeOf<FileUserDataBase>()
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdInEqualsItselfButNotStdOut(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                "return io.stdin == io.stdin, io.stdin ~= io.stdout, io.stdin == 1, io.stdin ~= 1"
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdOutIsFileUserDataHandle(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue stdout = script.DoString("return io.stdout");

            await Assert.That(stdout.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert
                .That(stdout.UserData.Object)
                .IsTypeOf<FileUserDataBase>()
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdErrIsFileUserDataHandle(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue stderr = script.DoString("return io.stderr");

            await Assert.That(stderr.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert
                .That(stderr.UserData.Object)
                .IsTypeOf<FileUserDataBase>()
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task RequireIoExposesSameStdHandles(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local io_module = require('io')
                return io_module.stdin == io.stdin, io_module.stdout == io.stdout
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task IoInputReturnsCurrentStdInHandle(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString("return io.input() == io.stdin");

            await Assert.That(result.CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task IoOutputReturnsCurrentStdOutHandle(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString("return io.output() == io.stdout");

            await Assert.That(result.CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdInCannotBeIndexedOrAssigned(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue indexResult = script.DoString("return io.stdin[1]");
            await Assert.That(indexResult.IsNil()).IsTrue().ConfigureAwait(false);

            ScriptRuntimeException assignException = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("io.stdin[1] = 42");
            });

            await Assert
                .That(assignException.Message)
                .Contains("attempt to index")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdOutCannotBeIndexedOrAssigned(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue indexResult = script.DoString("return io.stdout[1]");
            await Assert.That(indexResult.IsNil()).IsTrue().ConfigureAwait(false);

            ScriptRuntimeException assignException = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString("io.stdout[1] = 42");
            });

            await Assert
                .That(assignException.Message)
                .Contains("attempt to index")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdInArithmeticThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            await AssertThrowsAsync(script, "return io.stdin + 1", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin - 1", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin * 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin / 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin % 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin ^ 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return -io.stdin", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return #io.stdin", "attempt to get length of")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdOutArithmeticThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            await AssertThrowsAsync(script, "return io.stdout + 1", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout - 1", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout * 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout / 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout % 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout ^ 2", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return -io.stdout", "attempt to perform arithmetic")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return #io.stdout", "attempt to get length of")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdInConcatenationThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            await AssertThrowsAsync(script, "return io.stdin .. 'tail'", "attempt to concatenate")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdOutConcatenationThrows(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            await AssertThrowsAsync(script, "return io.stdout .. 'tail'", "attempt to concatenate")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdInComparisonsThrow(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            await AssertThrowsAsync(script, "return io.stdin < io.stdout", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin <= io.stdout", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin > io.stdout", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin >= io.stdout", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin < 0", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin <= 0", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin > 0", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdin >= 0", "attempt to compare")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StdOutComparisonsThrow(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            await AssertThrowsAsync(script, "return io.stdout < io.stdin", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout <= io.stdin", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout > io.stdin", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout >= io.stdin", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout < 0", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout <= 0", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout > 0", "attempt to compare")
                .ConfigureAwait(false);
            await AssertThrowsAsync(script, "return io.stdout >= 0", "attempt to compare")
                .ConfigureAwait(false);
        }

        private static async Task AssertThrowsAsync(Script script, string chunk, string messagePart)
        {
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString(chunk);
            });

            await Assert.That(exception.Message).Contains(messagePart).ConfigureAwait(false);
        }
    }
}
