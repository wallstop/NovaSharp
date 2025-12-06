namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class StringLibTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task StringGMatchConcatenatesWords()
        {
            string script =
                @"    
				t = '';

				for word in string.gmatch('Hello Lua user', '%a+') do 
					t = t .. word;
				end

				return (t);
				";

            DynValue result = Script.RunString(script);
            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("HelloLuauser");
        }

        [global::TUnit.Core.Test]
        public Task StringFindReturnsMatchStartAndEnd()
        {
            DynValue result = Script.RunString("return string.find('Hello Lua user', 'Lua');");
            return EndToEndDynValueAssert.ExpectAsync(result, 7, 9);
        }

        [global::TUnit.Core.Test]
        public Task StringFindReturnsNilWhenPatternMissing()
        {
            DynValue result = Script.RunString("return string.find('Hello Lua user', 'banana');");
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        public Task StringFindRespectsStartIndex()
        {
            DynValue result = Script.RunString("return string.find('Hello Lua user', 'Lua', 1);");
            return EndToEndDynValueAssert.ExpectAsync(result, 7, 9);
        }

        [global::TUnit.Core.Test]
        public Task StringFindRespectsStartIndexBeyondMatch()
        {
            DynValue result = Script.RunString("return string.find('Hello Lua user', 'Lua', 8);");
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        public Task StringFindSupportsNegativeStartIndices()
        {
            DynValue result = Script.RunString("return string.find('Hello Lua user', 'e', -5);");
            return EndToEndDynValueAssert.ExpectAsync(result, 13, 13);
        }

        [global::TUnit.Core.Test]
        public Task StringFindMatchesPatternsWithoutStart()
        {
            DynValue result = Script.RunString("return string.find('Hello Lua user', '%su');");
            return EndToEndDynValueAssert.ExpectAsync(result, 10, 11);
        }

        [global::TUnit.Core.Test]
        public Task StringFindMatchesPatternsWithStart()
        {
            DynValue result = Script.RunString("return string.find('Hello Lua user', '%su', 1);");
            return EndToEndDynValueAssert.ExpectAsync(result, 10, 11);
        }

        [global::TUnit.Core.Test]
        public Task StringFindHonorsPlainSearchFlag()
        {
            DynValue result = Script.RunString(
                "return string.find('Hello Lua user', '%su', 1, true);"
            );
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        public Task StringFindExtractsDateFromMessage()
        {
            string script =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%d%d/%d%d/%d%d%d%d';
				return s:sub(s:find(date));
			";

            DynValue result = Script.RunString(script);
            return EndToEndDynValueAssert.ExpectAsync(result, "30/05/1999");
        }

        [global::TUnit.Core.Test]
        public Task StringFindSupportsFrontierPatterns()
        {
            string script =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%f[%S]%d%d/%d%d/%d%d%d%d';
				return s:sub(s:find(date));
			";

            DynValue result = Script.RunString(script);
            return EndToEndDynValueAssert.ExpectAsync(result, "30/05/1999");
        }

        [global::TUnit.Core.Test]
        public Task StringFindFrontierPatternMissesWhitespaceBoundary()
        {
            string script =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%f[%s]%d%d/%d%d/%d%d%d%d';
				return s:find(date);
			";

            DynValue result = Script.RunString(script);
            return EndToEndDynValueAssert.ExpectAsync(result, null);
        }

        [global::TUnit.Core.Test]
        public Task StringFormatPadsNumericArguments()
        {
            string script =
                @"
				d = 5; m = 11; y = 1990
				return string.format('%02d/%02d/%04d', d, m, y)
			";

            DynValue result = Script.RunString(script);
            return EndToEndDynValueAssert.ExpectAsync(result, "05/11/1990");
        }

        [global::TUnit.Core.Test]
        public Task StringGSubDuplicatesWords()
        {
            string script =
                @"
				s = string.gsub('hello world', '(%w+)', '%1 %1')
				return s, s == 'hello hello world world'
			";

            DynValue result = Script.RunString(script);
            return EndToEndDynValueAssert.ExpectAsync(result, "hello hello world world", true);
        }

        [global::TUnit.Core.Test]
        public async Task PrintSupportsVariadicArguments()
        {
            string script =
                @"
				print('ciao', 1);
			";

            string printed = null;
            Script scriptHost = new();
            DynValue chunk = scriptHost.LoadString(script);
            scriptHost.Options.DebugPrint = s => printed = s;
            scriptHost.Call(chunk);

            await Assert.That(printed).IsEqualTo("ciao\t1");
        }

        [global::TUnit.Core.Test]
        public async Task PrintInvokesToStringMetamethods()
        {
            string script =
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
            Script scriptHost = new();
            DynValue chunk = scriptHost.LoadString(script);
            scriptHost.Options.DebugPrint = s => printed = s;
            scriptHost.Call(chunk);

            await Assert.That(printed).IsEqualTo("ciao\t1");
        }

        [global::TUnit.Core.Test]
        public Task ToStringMetamethodCanReturnVoid()
        {
            string script =
                @"
				t = {}
				mt = {}
				a = nil
				function mt.__tostring () a = 'yup' end
				setmetatable(t, mt)
				return tostring(t), a;
			";

            DynValue result = Script.RunString(script);
            return EndToEndDynValueAssert.ExpectAsync(result, DataType.Void, "yup");
        }

        [global::TUnit.Core.Test]
        public async Task StringGSubRejectsPercentEscapes()
        {
            string script = "string.gsub('hello world', '%w+', '%e')";

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
            {
                Script.RunString(script);
            });

            await Assert.That(exception.Message).Contains("invalid use of '%'");
        }

        [global::TUnit.Core.Test]
        public Task StringGSubHandlesMultilinePatterns()
        {
            Script script = new()
            {
                Globals =
                {
                    ["a"] =
                        @"                  'C:\temp\test.lua:68: bad argument #1 to 'date' (invalid conversion specifier '%Ja')'
    doesn't match '^[^:]+:%d+: bad argument #1 to 'date' %(invalid conversion specifier '%%Ja'%)'",
                },
            };

            string lua = "return string.gsub(a, '\\n', '\\n #')";
            DynValue result = script.DoString(lua);

            string original = script.Globals.Get("a").String;
            string expected = original.Replace("\n", "\n #", StringComparison.Ordinal);
            return EndToEndDynValueAssert.ExpectAsync(result, expected, 1);
        }

        [global::TUnit.Core.Test]
        public Task StringMatchMatchesErrorMessage()
        {
            string source = "test.lua:185: field 'day' missing in date table";
            string pattern = "^[^:]+:%d+: field 'day' missing in date table";
            return TestMatchAsync(source, pattern, expectedMatch: true);
        }

        private static async Task TestMatchAsync(string source, string pattern, bool expectedMatch)
        {
            Script script = new(CoreModules.StringLib);
            script.Globals["s"] = source;
            script.Globals["p"] = pattern;
            DynValue result = script.DoString("return string.match(s, p)");

            await Assert.That(!result.IsNil()).IsEqualTo(expectedMatch);
        }
    }
}
