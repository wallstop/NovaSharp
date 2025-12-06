namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib.IO;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    [ScriptGlobalOptionsIsolation]
    public sealed class IoStdHandleUserDataTUnitTests
    {
        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }

        [Test]
        public async Task StdInIsFileUserDataHandle()
        {
            Script script = CreateScript();
            DynValue stdin = script.DoString("return io.stdin");

            await Assert.That(stdin.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert
                .That(stdin.UserData.Object)
                .IsTypeOf<FileUserDataBase>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task StdInEqualsItselfButNotStdOut()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                "return io.stdin == io.stdin, io.stdin ~= io.stdout, io.stdin == 1, io.stdin ~= 1"
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task StdOutIsFileUserDataHandle()
        {
            Script script = CreateScript();
            DynValue stdout = script.DoString("return io.stdout");

            await Assert.That(stdout.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert
                .That(stdout.UserData.Object)
                .IsTypeOf<FileUserDataBase>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task StdErrIsFileUserDataHandle()
        {
            Script script = CreateScript();
            DynValue stderr = script.DoString("return io.stderr");

            await Assert.That(stderr.Type).IsEqualTo(DataType.UserData).ConfigureAwait(false);
            await Assert
                .That(stderr.UserData.Object)
                .IsTypeOf<FileUserDataBase>()
                .ConfigureAwait(false);
        }

        [Test]
        public async Task RequireIoExposesSameStdHandles()
        {
            Script script = CreateScript();
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
        public async Task IoInputReturnsCurrentStdInHandle()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return io.input() == io.stdin");

            await Assert.That(result.CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task IoOutputReturnsCurrentStdOutHandle()
        {
            Script script = CreateScript();
            DynValue result = script.DoString("return io.output() == io.stdout");

            await Assert.That(result.CastToBool()).IsTrue().ConfigureAwait(false);
        }

        [Test]
        public async Task StdInCannotBeIndexedOrAssigned()
        {
            Script script = CreateScript();

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
        public async Task StdOutCannotBeIndexedOrAssigned()
        {
            Script script = CreateScript();

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
        public async Task StdInArithmeticThrows()
        {
            Script script = CreateScript();

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
        public async Task StdOutArithmeticThrows()
        {
            Script script = CreateScript();

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
        public async Task StdInConcatenationThrows()
        {
            Script script = CreateScript();
            await AssertThrowsAsync(script, "return io.stdin .. 'tail'", "attempt to concatenate")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task StdOutConcatenationThrows()
        {
            Script script = CreateScript();
            await AssertThrowsAsync(script, "return io.stdout .. 'tail'", "attempt to concatenate")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task StdInComparisonsThrow()
        {
            Script script = CreateScript();

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
        public async Task StdOutComparisonsThrow()
        {
            Script script = CreateScript();

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
