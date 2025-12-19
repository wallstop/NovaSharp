namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class TableTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableAccessAndEmptyCtor(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("a = {} a[1] = 1 return a[1]");
            await EndToEndDynValueAssert.ExpectAsync(result, 1).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableAccessAndCtor(LuaCompatibilityVersion version)
        {
            string code =
                "a = { 55, 2, 3, aurevoir=6, [false] = 7 } "
                + "a[1] = 1; a.ciao = 4; a['hello'] = 5; "
                + "return a[1], a[2], a[3], a['ciao'], a.hello, a.aurevoir, a[false]";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert
                .ExpectAsync(result, 1, 2, 3, 4, 5, 6, 7)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableMethod1(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "x = 0 a = { value = 1912, val = function(self, num) x = self.value + num end } "
                    + "a.val(a, 82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableMethod2(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "x = 0 a = { value = 1912, val = function(self, num) x = self.value + num end } "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableMethod3(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "x = 0 a = { value = 1912 } function a.val(self, num) x = self.value + num end "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableMethod4(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "x = 0 local a = { value = 1912 } function a:val(num) x = self.value + num end "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableMethod5AllowsNestedPointerSyntax(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "x = 0 a = { value = 1912 } b = { tb = a } c = { tb = b } "
                    + "function c.tb.tb:val(num) x = self.value + num end "
                    + "a:val(82) return x"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, 1994).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableMethodChainingReturnsSelf(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "return (function() local a = {x=0} "
                    + "function a:add(x) self.x, a.y = self.x + x, 20; return self end "
                    + "return (a:add(10):add(20):add(30).x == 60 and a.y == 20) end)()"
            );
            await EndToEndDynValueAssert.ExpectAsync(result, true).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableNextWithMutation(LuaCompatibilityVersion version)
        {
            string code =
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

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, "1|2|3|4|5").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TablePairsAggregatesKeysAndValues(LuaCompatibilityVersion version)
        {
            string code =
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

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await Assert.That(result.Tuple[0].String.Length).IsEqualTo(5).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(15).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableIPairsStopsAfterBreak(LuaCompatibilityVersion version)
        {
            string code =
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

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 6, 12).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task LoadReturnsSyntaxError(LuaCompatibilityVersion version)
        {
            string code =
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

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await Assert.That(result.Tuple[0].IsNil()).IsTrue().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].Type)
                .IsEqualTo(DataType.String)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableSimplifiedAccesses(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue table = script.DoString("t = { ciao = 'hello' } return t");
            await Assert.That(table.Table["ciao"]).IsEqualTo("hello").ConfigureAwait(false);

            Script scriptWithGlobal = new Script(version, CoreModulePresets.Complete);
            scriptWithGlobal.Globals["x"] = "hello";
            DynValue tableWithRef = scriptWithGlobal.DoString("t = { ciao = x } return t");
            await Assert.That(tableWithRef.Table["ciao"]).IsEqualTo("hello").ConfigureAwait(false);

            Script empty = new Script(version, CoreModulePresets.Complete);
            DynValue created = empty.DoString("t = {} return t");
            empty.Globals["t", "ciao"] = "hello";
            await Assert.That(created.Table["ciao"]).IsEqualTo("hello").ConfigureAwait(false);

            Script assignAfter = new Script(version, CoreModulePresets.Complete);
            assignAfter.DoString("t = {}");
            assignAfter.Globals["t", "ciao"] = "hello";
            await Assert
                .That(assignAfter.Globals["t", "ciao"])
                .IsEqualTo("hello")
                .ConfigureAwait(false);

            Script readGlobal = new Script(version, CoreModulePresets.Complete);
            readGlobal.DoString("t = { ciao = 'hello' }");
            await Assert
                .That(readGlobal.Globals["t", "ciao"])
                .IsEqualTo("hello")
                .ConfigureAwait(false);

            Script nested = new Script(version, default(CoreModules));
            nested.DoString("t = { ciao = { 'hello' } }");
            await Assert
                .That(nested.Globals["t", "ciao", 1])
                .IsEqualTo("hello")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task NilRemovesEntryForPairs(LuaCompatibilityVersion version)
        {
            string code =
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

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, "id$$").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that table.unpack returns a tuple of values.
        /// table.unpack was moved from global unpack in Lua 5.2.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableUnpackReturnsTuple(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return table.unpack({3,4})");
            await EndToEndDynValueAssert.ExpectAsync(result, 3, 4).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrimeTableAllowsSimpleValues(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString("t = ${ ciao = 'hello' }");

            await Assert.That(script.Globals["t", "ciao"]).IsEqualTo("hello").ConfigureAwait(false);
            await Assert
                .That(script.Globals.Get("t").Table.OwnerScript == null)
                .IsTrue()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrimeTableBlocksFunctions(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("t = ${ ciao = function() end }")
            );
            await Task.CompletedTask.ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TableLengthCalculationsMirrorNunit()
        {
            // This test doesn't execute Lua code, just verifies Table class behavior
            Table table = new(null);

            await Assert.That(table.Length).IsEqualTo(0).ConfigureAwait(false);
            table.Set(1, DynValue.True);
            await Assert.That(table.Length).IsEqualTo(1).ConfigureAwait(false);

            table.Set(2, DynValue.True);
            table.Set(3, DynValue.True);
            table.Set(4, DynValue.True);
            await Assert.That(table.Length).IsEqualTo(4).ConfigureAwait(false);

            table.Set(3, DynValue.Nil);
            await Assert.That(table.Length).IsEqualTo(2).ConfigureAwait(false);

            table.Set(3, DynValue.True);
            await Assert.That(table.Length).IsEqualTo(4).ConfigureAwait(false);

            table.Set(3, DynValue.Nil);
            await Assert.That(table.Length).IsEqualTo(2).ConfigureAwait(false);

            table.Append(DynValue.True);
            await Assert.That(table.Length).IsEqualTo(4).ConfigureAwait(false);

            table.Append(DynValue.True);
            await Assert.That(table.Length).IsEqualTo(5).ConfigureAwait(false);
        }
    }
}
