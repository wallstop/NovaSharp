namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    /// <summary>
    /// Tests for the configurable VM stack ceiling that turns runaway recursion into a deterministic
    /// Lua <c>stack overflow</c> error instead of unbounded growth and host memory exhaustion.
    /// </summary>
    public sealed class VmStackCeilingTUnitTests
    {
        private static Script NewScript(
            LuaCompatibilityVersion version,
            ScriptOptions options = null
        )
        {
            options ??= new ScriptOptions();
            return new Script(new ScriptOptions(options) { CompatibilityVersion = version });
        }

        [Test]
        public async Task DefaultOptionsExposeVmStackCeilings()
        {
            ScriptOptions options = new();

            await Assert
                .That(options.MaxVmValueStackSize)
                .IsEqualTo(VmStackDefaults.ValueStackMaxCapacity)
                .ConfigureAwait(false);
            await Assert
                .That(options.MaxVmCallStackSize)
                .IsEqualTo(VmStackDefaults.ExecutionStackMaxCapacity)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task CopyConstructorPreservesVmStackCeilings()
        {
            ScriptOptions original = new()
            {
                MaxVmValueStackSize = 12345,
                MaxVmCallStackSize = 678,
            };

            ScriptOptions copy = new(original);

            await Assert.That(copy.MaxVmValueStackSize).IsEqualTo(12345).ConfigureAwait(false);
            await Assert.That(copy.MaxVmCallStackSize).IsEqualTo(678).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task InfiniteRecursionThrowsStackOverflow(LuaCompatibilityVersion version)
        {
            Script script = NewScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("local function f() return 1 + f() end return f()")
            );

            await Assert.That(exception.Message).Contains("stack overflow").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task StackOverflowIsCatchableViaPcall(LuaCompatibilityVersion version)
        {
            Script script = NewScript(version);

            DynValue result = script.DoString(
                @"
                local function f() return 1 + f() end
                local ok, err = pcall(f)
                return ok, err
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("stack overflow")
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task DeepBoundedRecursionSucceedsUnderDefaultCeiling(
            LuaCompatibilityVersion version
        )
        {
            Script script = NewScript(version);

            // ~20k non-tail frames is far below the default ceiling (~250k overflow depth) yet far deeper
            // than any realistic program, proving the default ceiling does not clip legitimate recursion.
            DynValue result = script.DoString(
                @"
                local function sum(n)
                    if n == 0 then return 0 end
                    return 1 + sum(n - 1)
                end
                return sum(20000)
                "
            );

            await Assert.That(result.Number).IsEqualTo(20000).ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task LowValueStackCeilingTripsEarly(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { MaxVmValueStackSize = 4096 };
            Script script = NewScript(version, options);

            // 4096 value slots overflow well before 100k frames, independent of per-frame slot count.
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                        local function f(n) return 1 + f(n + 1) end
                        return f(0)
                        "
                )
            );

            await Assert.That(exception.Message).Contains("stack overflow").ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task LowCallStackCeilingTripsEarly(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new() { MaxVmCallStackSize = 32 };
            Script script = NewScript(version, options);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                        local function f(n) return 1 + f(n + 1) end
                        return f(0)
                        "
                )
            );

            await Assert.That(exception.Message).Contains("stack overflow").ConfigureAwait(false);
        }
    }
}
