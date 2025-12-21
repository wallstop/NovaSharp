-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMethodsTUnitTests.cs:1168
-- @test: SomeOtherClassWithDualInterfaces.InteropTestNamesCamelized
-- @compat-notes: Uses injected variable: myobj
a = myobj:SomeMethodWithLongName(1);
				b = myobj:someMethodWithLongName(2);
				c = myobj:some_method_with_long_name(3);
				d = myobj:Some_method_withLong_name(4);
				
				return a + b + c + d;
