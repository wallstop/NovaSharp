namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class StringLibTests
    {
        [Test]
        public void StringGMatch1()
        {
            string script =
                @"    
				t = '';

				for word in string.gmatch('Hello Lua user', '%a+') do 
					t = t .. word;
				end

				return (t);
				";

            DynValue res = Script.RunString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("HelloLuauser"));
        }

        [Test]
        public void StringFind1()
        {
            string script = @"return string.find('Hello Lua user', 'Lua');";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, 7, 9);
        }

        [Test]
        public void StringFind2()
        {
            string script = @"return string.find('Hello Lua user', 'banana');";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, null);
        }

        [Test]
        public void StringFind3()
        {
            string script = @"return string.find('Hello Lua user', 'Lua', 1);";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, 7, 9);
        }

        [Test]
        public void StringFind4()
        {
            string script = @"return string.find('Hello Lua user', 'Lua', 8);";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, null);
        }

        [Test]
        public void StringFind5()
        {
            string script = @"return string.find('Hello Lua user', 'e', -5);";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, 13, 13);
        }

        [Test]
        public void StringFind6()
        {
            string script = @"return string.find('Hello Lua user', '%su');";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, 10, 11);
        }

        [Test]
        public void StringFind7()
        {
            string script = @"return string.find('Hello Lua user', '%su', 1);";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, 10, 11);
        }

        [Test]
        public void StringFind8()
        {
            string script = @"return string.find('Hello Lua user', '%su', 1, true);";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, null);
        }

        [Test]
        public void StringFind9()
        {
            string script =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%d%d/%d%d/%d%d%d%d';
				return s:sub(s:find(date));
			";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, "30/05/1999");
        }

        [Test]
        public void StringFind10()
        {
            string script =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%f[%S]%d%d/%d%d/%d%d%d%d';
				return s:sub(s:find(date));
			";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, "30/05/1999");
        }

        [Test]
        public void StringFind11()
        {
            string script =
                @"
				s = 'Deadline is 30/05/1999, firm'
				date = '%f[%s]%d%d/%d%d/%d%d%d%d';
				return s:find(date);
			";
            DynValue res = Script.RunString(script);
            Assert.That(res.IsNil(), Is.True);
        }

        [Test]
        public void StringFormat1()
        {
            string script =
                @"
				d = 5; m = 11; y = 1990
				return string.format('%02d/%02d/%04d', d, m, y)
			";
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, "05/11/1990");
        }

        [Test]
        public void StringGSub1()
        {
            string script =
                @"
				s = string.gsub('hello world', '(%w+)', '%1 %1')
				return s, s == 'hello hello world world'
			";
            DynValue res = Script.RunString(script);
            Assert.That("hello hello world world", Is.EqualTo(res.Tuple[0].String));
            Assert.That(true, Is.EqualTo(res.Tuple[1].Boolean));
        }

        [Test]
        public void PrintTest1()
        {
            string script =
                @"
				print('ciao', 1);
			";
            string printed = null;

            Script s = new();
            DynValue main = s.LoadString(script);

            s.Options.DebugPrint = s =>
            {
                printed = s;
            };

            s.Call(main);

            Assert.That(printed, Is.EqualTo("ciao\t1"));
        }

        [Test]
        public void PrintTest2()
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

            Script s = new();
            DynValue main = s.LoadString(script);

            s.Options.DebugPrint = s =>
            {
                printed = s;
            };

            s.Call(main);

            Assert.That(printed, Is.EqualTo("ciao\t1"));
        }

        [Test]
        public void ToStringTest()
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
            DynValue res = Script.RunString(script);
            EndToEndUtils.DynAssert(res, DataType.Void, "yup");
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void StringGSub2()
        {
            string script =
                @"
				string.gsub('hello world', '%w+', '%e')
			";
            DynValue res = Script.RunString(script);
        }

        [Test]
        public void StringGSub3()
        {
            Script s = new()
            {
                Globals =
                {
                    ["a"] =
                        @"                  'C:\temp\test.lua:68: bad argument #1 to 'date' (invalid conversion specifier '%Ja')'
    doesn't match '^[^:]+:%d+: bad argument #1 to 'date' %(invalid conversion specifier '%%Ja'%)'",
                },
            };

            string script =
                @"
				string.gsub(a, '\n', '\n #')
			";
            DynValue res = s.DoString(script);
        }

        [Test]
        public void StringMatch1()
        {
            string s = @"test.lua:185: field 'day' missing in date table";
            string p = @"^[^:]+:%d+: field 'day' missing in date table";

            TestMatch(s, p, true);
        }

        private void TestMatch(string s, string p, bool expected)
        {
            Script script = new(CoreModules.StringLib);
            script.Globals["s"] = s;
            script.Globals["p"] = p;
            DynValue res = script.DoString("return string.match(s, p)");

            Assert.That(!res.IsNil(), Is.EqualTo(expected));
        }
    }
}
