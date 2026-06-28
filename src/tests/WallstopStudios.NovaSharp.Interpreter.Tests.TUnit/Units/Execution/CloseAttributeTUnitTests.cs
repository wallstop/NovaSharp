namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class CloseAttributeTUnitTests
    {
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ToBeClosedVariablesCloseInReverseOrderOnScopeExit(
            LuaCompatibilityVersion version
        )
        {
            // <close> attribute is Lua 5.4+
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                local function run()
                    local first <close> = newcloser('first')
                    local second <close> = newcloser('second')
                end

                run()
                return log
                "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            Table log = result.Table;
            await Assert.That(log.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("second:nil").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("first:nil").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ReassignmentClosesPreviousValueImmediately(
            LuaCompatibilityVersion version
        )
        {
            // <close> attribute is Lua 5.4+
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                local function run()
                    local target <close> = newcloser('first')
                    target = newcloser('second')
                end

                run()
                return log
                "
            );

            Table log = result.Table;
            await Assert.That(log.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("first:nil").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("second:nil").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task ErrorPathPassesErrorObjectToCloseMetamethod(
            LuaCompatibilityVersion version
        )
        {
            // <close> attribute is Lua 5.4+
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local captured = {}

                local function newcloser()
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            captured.err = err
                        end
                    })
                    return token
                end

                local function run()
                    local _ <close> = newcloser()
                    error('boom')
                end

                local ok, message = pcall(run)
                return ok, message, captured.err
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).Contains("boom").ConfigureAwait(false);
            await Assert.That(result.Tuple[2].String).Contains("boom").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task MissingCloseMetamethodRaisesRuntimeError(LuaCompatibilityVersion version)
        {
            // <close> attribute is Lua 5.4+
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local ok, err = pcall(function()
                    local _ <close> = {}
                end)
                return ok, err
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("__close metamethod expected")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task GotoJumpOutOfScopeClosesLocals(LuaCompatibilityVersion version)
        {
            // <close> attribute is Lua 5.4+
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                do
                    local outer <close> = newcloser('outer')
                    do
                        local inner <close> = newcloser('inner')
                        goto finish
                    end
                end

                ::finish::
                return log
                "
            );

            Table log = result.Table;
            await Assert.That(log.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("inner:nil").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("outer:nil").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task BreakStatementClosesLoopScopedLocals(LuaCompatibilityVersion version)
        {
            // <close> attribute is Lua 5.4+
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                        end
                    })
                    return token
                end

                for i = 1, 3 do
                    local closer <close> = newcloser('loop_' .. i)
                    break
                end

                return log
                "
            );

            Table log = result.Table;
            await Assert.That(log.Length).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("loop_1:nil").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task CloseMetamethodErrorsAreCapturedAndOtherClosersRun(
            LuaCompatibilityVersion version
        )
        {
            // <close> attribute is Lua 5.4+
            Script script = new(version);
            DynValue result = script.DoString(
                @"
                local log = {}

                local function newcloser(name, should_error)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                            if should_error then
                                error('close:' .. name, 0)
                            end
                        end
                    })
                    return token
                end

                local function run()
                    local first <close> = newcloser('first', true)
                    local second <close> = newcloser('second', false)
                end

                local ok, err = pcall(run)
                return ok, err, log
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].String).Contains("close:first").ConfigureAwait(false);

            Table log = result.Tuple[2].Table;
            await Assert.That(log.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(log.Get(1).String).IsEqualTo("second:nil").ConfigureAwait(false);
            await Assert.That(log.Get(2).String).IsEqualTo("first:nil").ConfigureAwait(false);
        }
    }
}
