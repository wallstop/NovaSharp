namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class StringLibTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringGMatchConcatenatesWords(LuaCompatibilityVersion version)
        {
            string code =
                @"    
				t = '';

				for word in string.gmatch('Hello Lua user', '%a+') do 
					t = t .. word;
				end

				return (t);
				";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("HelloLuauser");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindReturnsMatchStartAndEnd(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.find('Hello Lua user', 'Lua');");
            return EndToEndDynValueAssert.ExpectAsync(result, 7, 9);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindReturnsNilWhenPatternMissing(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.find('Hello Lua user', 'banana');");
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindRespectsStartIndex(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.find('Hello Lua user', 'Lua', 1);");
            return EndToEndDynValueAssert.ExpectAsync(result, 7, 9);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindRespectsStartIndexBeyondMatch(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.find('Hello Lua user', 'Lua', 8);");
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindSupportsNegativeStartIndices(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.find('Hello Lua user', 'e', -5);");
            return EndToEndDynValueAssert.ExpectAsync(result, 13, 13);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindMatchesPatternsWithoutStart(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.find('Hello Lua user', '%su');");
            return EndToEndDynValueAssert.ExpectAsync(result, 10, 11);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindMatchesPatternsWithStart(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return string.find('Hello Lua user', '%su', 1);");
            return EndToEndDynValueAssert.ExpectAsync(result, 10, 11);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindHonorsPlainSearchFlag(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(
                "return string.find('Hello Lua user', '%su', 1, true);"
            );
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindExtractsDateFromMessage(LuaCompatibilityVersion version)
        {
            string code =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%d%d/%d%d/%d%d%d%d';
                return s:sub(s:find(date));
			";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            return EndToEndDynValueAssert.ExpectAsync(result, "30/05/1999");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindSupportsFrontierPatterns(LuaCompatibilityVersion version)
        {
            string code =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%f[%S]%d%d/%d%d/%d%d%d%d';
                return s:sub(s:find(date));
			";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            return EndToEndDynValueAssert.ExpectAsync(result, "30/05/1999");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFindFrontierPatternMissesWhitespaceBoundary(
            LuaCompatibilityVersion version
        )
        {
            string code =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%f[%s]%d%d/%d%d/%d%d%d%d';
                return s:find(date);
			";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringFormatPadsNumericArguments(LuaCompatibilityVersion version)
        {
            string code =
                @"
				d = 5; m = 11; y = 1990
                return string.format('%02d/%02d/%04d', d, m, y)
			";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            return EndToEndDynValueAssert.ExpectAsync(result, "05/11/1990");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringGSubDuplicatesWords(LuaCompatibilityVersion version)
        {
            string code =
                @"
				s = string.gsub('hello world', '(%w+)', '%1 %1')
                return s, s == 'hello hello world world'
			";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            return EndToEndDynValueAssert.ExpectAsync(result, "hello hello world world", true);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrintSupportsVariadicArguments(LuaCompatibilityVersion version)
        {
            string code =
                @"
				print('ciao', 1);
			";

            string printed = null;
            Script scriptHost = new Script(version, CoreModulePresets.Complete);
            DynValue chunk = scriptHost.LoadString(code);
            scriptHost.Options.DebugPrint = s => printed = s;
            scriptHost.Call(chunk);

            await Assert.That(printed).IsEqualTo("ciao\t1");
        }

        // print/__tostring CLR boundary detection behaves differently in pre-5.4 versions
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task PrintInvokesToStringMetamethods(LuaCompatibilityVersion version)
        {
            string code =
                @"
				t = {};
				m = {};

				function m.__tostring()
					return 'ciao';
				end

				setmetatable(t, m);

				print(t, 1);
			";

            string printed = null;
            Script scriptHost = new Script(version, CoreModulePresets.Complete);
            DynValue chunk = scriptHost.LoadString(code);
            scriptHost.Options.DebugPrint = s => printed = s;
            scriptHost.Call(chunk);

            await Assert.That(printed).IsEqualTo("ciao\t1");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task ToStringMetamethodCanReturnVoid(LuaCompatibilityVersion version)
        {
            string code =
                @"
				t = {}
				mt = {}
				a = nil
				function mt.__tostring () a = 'yup' end
				setmetatable(t, mt)
                return tostring(t), a;
			";

            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString(code);
            return EndToEndDynValueAssert.ExpectAsync(result, DataType.Void, "yup");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StringGSubRejectsPercentEscapes(LuaCompatibilityVersion version)
        {
            string code = "string.gsub('hello world', '%w+', '%e')";

            Script script = new Script(version, CoreModulePresets.Complete);
            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                script.DoString(code);
            });

            await Assert.That(exception.Message).Contains("invalid use of '%'");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringGSubHandlesMultilinePatterns(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.Globals["a"] =
                @"                  'C:\temp\test.lua:68: bad argument #1 to 'date' (invalid conversion specifier '%Ja')'
    doesn't match '^[^:]+:%d+: bad argument #1 to 'date' %(invalid conversion specifier '%%Ja'%)'";

            string lua = "return string.gsub(a, '\\n', '\\n #')";
            DynValue result = script.DoString(lua);

            string original = script.Globals.Get("a").String;
            string expected = original.Replace("\n", "\n #", StringComparison.Ordinal);
            return EndToEndDynValueAssert.ExpectAsync(result, expected, 1);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public Task StringMatchMatchesErrorMessage(LuaCompatibilityVersion version)
        {
            string source = "test.lua:185: field 'day' missing in date table";
            string pattern = "^[^:]+:%d+: field 'day' missing in date table";
            return TestMatchAsync(version, source, pattern, expectedMatch: true);
        }

        private static async Task TestMatchAsync(
            LuaCompatibilityVersion version,
            string source,
            string pattern,
            bool expectedMatch
        )
        {
            Script script = new Script(version, CoreModules.StringLib);
            script.Globals["s"] = source;
            script.Globals["p"] = pattern;
            DynValue result = script.DoString("return string.match(s, p)");

            await Assert.That(!result.IsNil()).IsEqualTo(expectedMatch);
        }
    }
}
