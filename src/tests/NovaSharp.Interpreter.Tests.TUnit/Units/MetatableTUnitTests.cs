namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

    public sealed class MetatableTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task IndexMetatableResolvesMissingKeys()
        {
            Script script = new();
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
        public async Task MetatableRawAccessStillRespectsMetatable()
        {
            Script script = new(CoreModules.Metatables | CoreModules.Basic);

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
        public async Task CallMetatableAggregatesState()
        {
            Script script = new(CoreModules.PresetComplete);

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
        public async Task PairsMetamethodControlsIteration()
        {
            Script script = new(CoreModules.PresetComplete);

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
        public async Task ProtectedMetatablePreventsMutation()
        {
            Script script = new(CoreModules.PresetComplete);

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
    }
}
