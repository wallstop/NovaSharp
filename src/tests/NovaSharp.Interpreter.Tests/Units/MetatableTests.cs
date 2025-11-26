namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class MetatableTests
    {
        [Test]
        public void IndexMetatableResolvesMissingKeys()
        {
            Script script = new();
            Table table = new(script);
            Table metatable = new(script)
            {
                ["__index"] = DynValue.NewCallback(
                    (_, args) =>
                    {
                        string key = args[1].CastToString();
                        return DynValue.NewString($"missing:{key}");
                    }
                ),
            };

            table.MetaTable = metatable;
            script.Globals["subject"] = table;

            DynValue result = script.DoString("return subject.someKey");

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.String));
                Assert.That(result.String, Is.EqualTo("missing:someKey"));
            });
        }

        [Test]
        public void MetatableRawAccessStillRespectsMetatable()
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
            Assert.That(subject.Get("value").Number, Is.EqualTo(10));

            subject.Set("value", DynValue.NewNumber(7));
            Assert.That(subject.Get("value").Number, Is.EqualTo(7));
        }

        [Test]
        public void CallMetatableAggregatesState()
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

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(3));
                Assert.That(second.Number, Is.EqualTo(5));
                Assert.That(total.Number, Is.EqualTo(5));
            });
        }

        [Test]
        public void PairsMetamethodControlsIteration()
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
            Assert.That(result.String, Is.EqualTo("virtual=42"));
        }

        [Test]
        public void ProtectedMetatablePreventsMutation()
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
            Assert.That(meta.String, Is.EqualTo("locked"));

            DynValue pcallResult = script.DoString(
                "return pcall(function() setmetatable(subject, {}) end)"
            );

            Assert.Multiple(() =>
            {
                Assert.That(pcallResult.Tuple.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(pcallResult.Tuple[0].Boolean, Is.False);
                Assert.That(
                    pcallResult.Tuple[1].String,
                    Does.Contain("cannot change a protected metatable")
                );
            });
        }
    }
}
