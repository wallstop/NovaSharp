namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Tree.Statements
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class BreakStatementTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BreakOutsideLoopThrowsSyntaxError(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString("break")
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("not inside a loop")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BreakInsideStandaloneFunctionTriggersLoopBoundaryGuard(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            const string chunk =
                @"
                local function inner()
                    break
                end
            ";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString(chunk)
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("not inside a loop")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BreakInsideNestedFunctionDefinedInLoopCannotEscapeOuterLoop(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            const string chunk =
                @"
                for i = 1, 2 do
                    local function inner()
                        break
                    end
                end
            ";

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                script.DoString(chunk)
            );

            await Assert
                .That(exception.DecoratedMessage)
                .Contains("not inside a loop")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MultipleBreakStatementsRespectInnermostLoopScope(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);
            const string chunk =
                @"
                local log = {}

                local function emit(flag)
                    while true do
                        if flag then
                            table.insert(log, 'first')
                            break
                        end

                        table.insert(log, 'second')
                        break
                    end
                end

                emit(true)
                emit(false)
                return log
            ";

            DynValue result = script.DoString(chunk);
            Table log = result.Table;

            await Assert.That(log.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("first").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("second").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task BreakOnlyExitsInnermostLoop(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            const string chunk =
                @"
                local log = {}
                for outer = 1, 3 do
                    for inner = 1, 3 do
                        table.insert(log, string.format('%d-%d', outer, inner))
                        break
                    end
                end
                return log
            ";

            DynValue result = script.DoString(chunk);
            Table log = result.Table;

            await Assert.That(log.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("1-1").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("2-1").ConfigureAwait(false);
            await Assert.That(log.Get(3).String).IsEqualTo("3-1").ConfigureAwait(false);
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new(Script.DefaultOptions) { CompatibilityVersion = version };
            return new Script(CoreModulePresets.Complete, options);
        }
    }
}
