namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class TableModuleTests
    {
        [Test]
        public void PackPreservesNilAndReportsCount()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local t = table.pack('a', nil, 42)
                return t.n, t[1], t[2], t[3]
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(4));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(3));
                Assert.That(result.Tuple[1].String, Is.EqualTo("a"));
                Assert.That(result.Tuple[2].IsNil(), Is.True);
                Assert.That(result.Tuple[3].Number, Is.EqualTo(42));
            });
        }

        [Test]
        public void UnpackHonorsExplicitBounds()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 10, 20, 30, 40 }
                return table.unpack(values, 2, 3)
                "
            );

            Assert.That(result.Tuple.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(20));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(30));
            });
        }

        [Test]
        public void SortNumbersUsesDefaultComparer()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 4, 1, 3 }
                table.sort(values)
                return values[1], values[2], values[3]
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(1));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(3));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(4));
            });
        }

        [Test]
        public void SortThrowsWhenComparatorIsInvalidType()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local values = { 1, 2 }
                    table.sort(values, {})
                    "
                )
            );

            Assert.That(exception.Message, Does.Contain("bad argument #2 to 'sort'"));
        }

        [Test]
        public void SortUsesMetamethodWhenComparerMissing()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local mt = {}
                function mt.__lt(left, right)
                    return left.value < right.value
                end

                local values = {
                    setmetatable({ value = 3 }, mt),
                    setmetatable({ value = 1 }, mt),
                    setmetatable({ value = 2 }, mt)
                }

                table.sort(values)
                return values[1].value, values[2].value, values[3].value
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Tuple[0].Number, Is.EqualTo(1));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(2));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(3));
            });
        }

        [Test]
        public void SortTreatsComparatorFalseResultsAsEqual()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 3, 1 }
                table.sort(values, function(a, b)
                    return false
                end)
                return values[1], values[2]
                "
            );

            double first = result.Tuple[0].Number;
            double second = result.Tuple[1].Number;

            Assert.Multiple(() =>
            {
                Assert.That(first + second, Is.EqualTo(4));
                Assert.That(first * second, Is.EqualTo(3));
            });
        }

        [Test]
        public void SortThrowsWhenValuesHaveNoNaturalOrder()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    table.sort({ true, false })
                    "
                )
            );

            Assert.That(exception.Message, Does.Contain("attempt to compare"));
        }

        [Test]
        public void SortPropagatesErrorsRaisedByComparator()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local values = { 1, 2 }
                    table.sort(values, function()
                        error('sort failed')
                    end)
                    "
                )
            );

            Assert.That(exception.Message, Does.Contain("sort failed"));
        }

        [Test]
        public void InsertValidatesPositionType()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local values = {}
                    table.insert(values, 'two', 99)
                    "
                )
            );

            Assert.That(exception.Message, Does.Contain("table.insert"));
        }

        [Test]
        public void RemoveRejectsExtraArguments()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local values = { 1, 2, 3 }
                    table.remove(values, 1, 2)
                    "
                )
            );

            Assert.That(exception.Message, Does.Contain("wrong number of arguments to 'remove'"));
        }

        [Test]
        public void InsertUsesLenMetamethodWhenPresent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = setmetatable({ [1] = 'seed' }, {
                    __len = function()
                        return 4
                    end
                })

                table.insert(values, 'sentinel')
                return values[5]
                "
            );

            Assert.That(result.String, Is.EqualTo("sentinel"));
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
