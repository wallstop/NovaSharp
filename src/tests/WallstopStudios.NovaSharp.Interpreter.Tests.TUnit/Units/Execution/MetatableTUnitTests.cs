namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class MetatableTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task IndexMetatableResolvesMissingKeys(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            Table table = new(script);
            Table metatable = new(script)
            {
                ["__index"] = DynValue.NewCallback(
                    (_, args) => DynValue.NewString($"missing:{args[1].CastToString()}")
                ),
            };

            table.MetaTable = metatable;
            script.Globals["subject"] = table;

            DynValue result = script.DoString("return subject.someKey");

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("missing:someKey").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MetatableRawAccessStillRespectsMetatable(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            script.DoString(
                @"
                subject = {}
                setmetatable(subject, {
                    __newindex = function(t, key, value)
                        rawset(t, key, value * 2)
                    end
                })

                subject.value = 5
            "
            );

            Table subject = script.Globals.Get("subject").Table;
            await Assert.That(subject.Get("value").Number).IsEqualTo(10).ConfigureAwait(false);

            subject.Set("value", DynValue.NewNumber(7));
            await Assert.That(subject.Get("value").Number).IsEqualTo(7).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallMetatableAggregatesState(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            script.DoString(
                @"
                subject = setmetatable({ total = 0 }, {
                    __call = function(self, amount)
                        self.total = self.total + amount
                        return self.total
                    end
                })
                "
            );

            DynValue first = script.DoString("return subject(3)");
            DynValue second = script.DoString("return subject(2)");
            DynValue total = script.DoString("return subject.total");

            await Assert.That(first.Number).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(second.Number).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(total.Number).IsEqualTo(5).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua53)]
        public async Task TableValuedCallMetamethodDoesNotChainBeforeLua54(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local target = {}
                    local proxy = {}
                    setmetatable(target, { __call = proxy })
                    setmetatable(proxy, {
                        __call = function()
                            return 'unexpected'
                        end
                    })
                    return target()
                "
                )
            );

            await Assert.That(exception.Message).Contains("attempt to call").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua54)]
        public async Task TableValuedCallMetamethodChainsFromLua54(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            DynValue result = script.DoString(
                @"
                local target = {}
                local proxy = {}
                setmetatable(target, { __call = proxy })
                setmetatable(proxy, {
                    __call = function(...)
                        local a, b, c = ...
                        return select('#', ...), a == proxy, b == target, c == nil
                    end
                })
                return target()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TableValuedCallMetamethodChainsInLatestDefault()
        {
            Script script = new(CoreModulePresets.Complete);

            DynValue result = script.DoString(
                @"
                local target = {}
                local proxy = {}
                setmetatable(target, { __call = proxy })
                setmetatable(proxy, {
                    __call = function(...)
                        local a, b, c = ...
                        return select('#', ...), a == proxy, b == target, c == nil
                    end
                })
                return target()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PairsMetamethodControlsIteration(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            script.DoString(
                @"
                subject = setmetatable({}, {
                    __pairs = function(self)
                        local yielded = false
                        return function(_, state)
                            if yielded then
                                return nil
                            end
                            yielded = true
                            return 'virtual', 42
                        end, self, nil
                    end
                })

                subject.a = 1
                collected = {}
                for k, v in pairs(subject) do
                    table.insert(collected, k .. '=' .. v)
                end
                "
            );

            DynValue result = script.DoString("return table.concat(collected, ',')");
            await Assert.That(result.String).IsEqualTo("virtual=42").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ProtectedMetatablePreventsMutation(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);

            script.DoString(
                @"
                subject = {}
                setmetatable(subject, {
                    __metatable = 'locked'
                })
                "
            );

            DynValue meta = script.DoString("return getmetatable(subject)");
            await Assert.That(meta.String).IsEqualTo("locked").ConfigureAwait(false);

            DynValue pcallResult = script.DoString(
                "return pcall(function() setmetatable(subject, {}) end)"
            );

            await Assert
                .That(pcallResult.Tuple.Length)
                .IsGreaterThanOrEqualTo(2)
                .ConfigureAwait(false);
            await Assert.That(pcallResult.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(pcallResult.Tuple[1].String)
                .Contains("cannot change a protected metatable")
                .ConfigureAwait(false);
        }

        private static Script CreateScript(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new(Script.DefaultOptions) { CompatibilityVersion = version };
            return new Script(CoreModulePresets.Complete, options);
        }
    }
}
