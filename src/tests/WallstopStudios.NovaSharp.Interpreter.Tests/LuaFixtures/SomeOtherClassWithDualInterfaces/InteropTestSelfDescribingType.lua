-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMethodsTUnitTests.cs:1197
-- @test: SomeOtherClassWithDualInterfaces.InteropTestSelfDescribingType
-- @compat-notes: Uses injected variable: myobj
a = myobj[1];
				b = myobj[2];
				c = myobj[3];
				
				return a + b + c;
