#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class TableModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PackPreservesNilAndReportsCount()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local t = table.pack('a', nil, 42)
                return t.n, t[1], t[2], t[3]
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(4);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[1].String).IsEqualTo("a");
            await Assert.That(result.Tuple[2].IsNil()).IsTrue();
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task UnpackHonorsExplicitBounds()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 10, 20, 30, 40 }
                return table.unpack(values, 2, 3)
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(20);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(30);
        }

        [global::TUnit.Core.Test]
        public async Task SortNumbersUsesDefaultComparer()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 4, 1, 3 }
                table.sort(values)
                return values[1], values[2], values[3]
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(4);
        }

        [global::TUnit.Core.Test]
        public async Task SortThrowsWhenComparatorIsInvalidType()
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

            await Assert.That(exception.Message).Contains("bad argument #2 to 'sort'");
        }

        [global::TUnit.Core.Test]
        public async Task SortUsesMetamethodWhenComparerMissing()
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

            await Assert.That(result.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task SortTreatsComparatorFalseResultsAsEqual()
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

            await Assert.That(first + second).IsEqualTo(4d);
            await Assert.That(first * second).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        public async Task SortThrowsWhenValuesHaveNoNaturalOrder()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    table.sort({ true, false })
                    "
                )
            );

            await Assert.That(exception.Message).Contains("attempt to compare");
        }

        [global::TUnit.Core.Test]
        public async Task SortPropagatesErrorsRaisedByComparator()
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

            await Assert.That(exception.Message).Contains("sort failed");
        }

        [global::TUnit.Core.Test]
        public async Task InsertValidatesPositionType()
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

            await Assert.That(exception.Message).Contains("table.insert");
        }

        [global::TUnit.Core.Test]
        public async Task RemoveRejectsExtraArguments()
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

            await Assert.That(exception.Message).Contains("wrong number of arguments to 'remove'");
        }

        [global::TUnit.Core.Test]
        public async Task InsertUsesLenMetamethodWhenPresent()
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

            await Assert.That(result.String).IsEqualTo("sentinel");
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
#pragma warning restore CA2007
