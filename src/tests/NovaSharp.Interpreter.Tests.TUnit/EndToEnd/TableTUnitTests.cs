#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;

    public sealed class TableTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TableAccessAndEmptyCtor()
        {
            DynValue result = Script.RunString("a = {} a[1] = 1 return a[1]");
            await EndToEndDynValueAssert.ExpectAsync(result, 1);
        }

        [global::TUnit.Core.Test]
        public async Task TableAccessAndCtor()
        {
            string script =
                "a = { 55, 2, 3, aurevoir=6, [false] = 7 } "
                + "a[1] = 1; a.ciao = 4; a['hello'] = 5; "
                + "return a[1], a[2], a[3], a['ciao'], a.hello, a.aurevoir, a[false]";

            DynValue result = Script.RunString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 1, 2, 3, 4, 5, 6, 7);
        }

        [global::TUnit.Core.Test]
        public async Task TableMethod1()
        {
            DynValue result = Script.RunString(
                "x = 0 a = { value = 1912, val = function(self, num) x = self.value + num end } "
                    + "a.val(a, 82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994);
        }

        [global::TUnit.Core.Test]
        public async Task TableMethod2()
        {
            DynValue result = Script.RunString(
                "x = 0 a = { value = 1912, val = function(self, num) x = self.value + num end } "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994);
        }

        [global::TUnit.Core.Test]
        public async Task TableMethod3()
        {
            DynValue result = Script.RunString(
                "x = 0 a = { value = 1912 } function a.val(self, num) x = self.value + num end "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994);
        }

        [global::TUnit.Core.Test]
        public async Task TableMethod4()
        {
            DynValue result = Script.RunString(
                "x = 0 local a = { value = 1912 } function a:val(num) x = self.value + num end "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994);
        }

        [global::TUnit.Core.Test]
        public async Task TableMethod5AllowsNestedPointerSyntax()
        {
            DynValue result = Script.RunString(
                "x = 0 a = { value = 1912 } b = { tb = a } c = { tb = b } "
                    + "function c.tb.tb:val(num) x = self.value + num end "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994);
        }

        [global::TUnit.Core.Test]
        public async Task TableMethodChainingReturnsSelf()
        {
            DynValue result = Script.RunString(
                "return (function() local a = {x=0} "
                    + "function a:add(x) self.x, a.y = self.x + x, 20; return self end "
                    + "return (a:add(10):add(20):add(30).x == 60 and a.y == 20) end)()"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, true);
        }

        [global::TUnit.Core.Test]
        public async Task TableNextWithMutation()
        {
            string script =
                @"
                x = {}
                function copy(k, v) x[k] = v end
                t = { a = 1, b = 2, c = 3, d = 4, e = 5 }
                k,v = next(t, nil); copy(k, v);
                k,v = next(t, k); copy(k, v); v = nil;
                k,v = next(t, k); copy(k, v);
                k,v = next(t, k); copy(k, v);
                k,v = next(t, k); copy(k, v);
                return x.a .. '|' .. x.b .. '|' .. x.c .. '|' .. x.d .. '|' .. x.e
            ";

            DynValue result = Script.RunString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "1|2|3|4|5");
        }

        [global::TUnit.Core.Test]
        public async Task TablePairsAggregatesKeysAndValues()
        {
            string script =
                @"
                V = 0
                K = ''
                t = { a = 1, b = 2, c = 3, d = 4, e = 5 }
                for k, v in pairs(t) do
                    K = K .. k
                    V = V + v
                end
                return K, V
            ";

            DynValue result = Script.RunString(script);
            await Assert.That(result.Tuple[0].String.Length).IsEqualTo(5);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(15);
        }

        [global::TUnit.Core.Test]
        public async Task TableIPairsStopsAfterBreak()
        {
            string script =
                @"
                x = 0
                y = 0
                t = { 2, 4, 6, 8, 10, 12 }
                for i,j in ipairs(t) do
                    x = x + i
                    y = y + j
                    if (i >= 3) then break end
                end
                return x, y
            ";

            DynValue result = Script.RunString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 6, 12);
        }

        [global::TUnit.Core.Test]
        public async Task LoadReturnsSyntaxError()
        {
            string script =
                @"
                function reader()
                    i = i + 1
                    return t[i]
                end
                t = { [[?syntax error?]] }
                i = 0
                f, msg = load(reader, 'errorchunk')
                return f, msg
            ";

            DynValue result = Script.RunString(script);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue();
            await Assert.That(result.Tuple[1].Type).IsEqualTo(DataType.String);
        }

        [global::TUnit.Core.Test]
        public async Task TableSimplifiedAccesses()
        {
            Script script = new();
            DynValue table = script.DoString("t = { ciao = 'hello' } return t");
            await Assert.That(table.Table["ciao"]).IsEqualTo("hello");

            Script scriptWithGlobal = new() { Globals = { ["x"] = "hello" } };
            DynValue tableWithRef = scriptWithGlobal.DoString("t = { ciao = x } return t");
            await Assert.That(tableWithRef.Table["ciao"]).IsEqualTo("hello");

            Script empty = new();
            DynValue created = empty.DoString("t = {} return t");
            empty.Globals["t", "ciao"] = "hello";
            await Assert.That(created.Table["ciao"]).IsEqualTo("hello");

            Script assignAfter = new();
            assignAfter.DoString("t = {}");
            assignAfter.Globals["t", "ciao"] = "hello";
            await Assert.That(assignAfter.Globals["t", "ciao"]).IsEqualTo("hello");

            Script readGlobal = new();
            readGlobal.DoString("t = { ciao = 'hello' }");
            await Assert.That(readGlobal.Globals["t", "ciao"]).IsEqualTo("hello");

            Script nested = new(default(CoreModules));
            nested.DoString("t = { ciao = { 'hello' } }");
            await Assert.That(nested.Globals["t", "ciao", 1]).IsEqualTo("hello");
        }

        [global::TUnit.Core.Test]
        public async Task NilRemovesEntryForPairs()
        {
            string script =
                @"
                str = ''
                function showTable(t)
                    for i,j in pairs(t) do
                        str = str .. i
                    end
                    str = str .. '$'
                end
                tb = {}
                tb['id'] = 3
                showTable(tb)
                tb['id'] = nil
                showTable(tb)
                return str
            ";

            DynValue result = Script.RunString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "id$$");
        }

        [global::TUnit.Core.Test]
        public async Task TableUnpackReturnsTuple()
        {
            DynValue result = Script.RunString("return unpack({3,4})");
            await EndToEndDynValueAssert.ExpectAsync(result, 3, 4);
        }

        [global::TUnit.Core.Test]
        public async Task PrimeTableAllowsSimpleValues()
        {
            Script script = new();
            script.DoString("t = ${ ciao = 'hello' }");

            await Assert.That(script.Globals["t", "ciao"]).IsEqualTo("hello");
            await Assert.That(script.Globals.Get("t").Table.OwnerScript == null).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task PrimeTableBlocksFunctions()
        {
            Script script = new();
            Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("t = ${ ciao = function() end }")
            );
            await Task.CompletedTask;
        }

        [global::TUnit.Core.Test]
        public async Task TableLengthCalculationsMirrorNunit()
        {
            Table table = new(null);

            await Assert.That(table.Length).IsEqualTo(0);
            table.Set(1, DynValue.True);
            await Assert.That(table.Length).IsEqualTo(1);

            table.Set(2, DynValue.True);
            table.Set(3, DynValue.True);
            table.Set(4, DynValue.True);
            await Assert.That(table.Length).IsEqualTo(4);

            table.Set(3, DynValue.Nil);
            await Assert.That(table.Length).IsEqualTo(2);

            table.Set(3, DynValue.True);
            await Assert.That(table.Length).IsEqualTo(4);

            table.Set(3, DynValue.Nil);
            await Assert.That(table.Length).IsEqualTo(2);

            table.Append(DynValue.True);
            await Assert.That(table.Length).IsEqualTo(4);

            table.Append(DynValue.True);
            await Assert.That(table.Length).IsEqualTo(5);
        }
    }
}
#pragma warning restore CA2007
