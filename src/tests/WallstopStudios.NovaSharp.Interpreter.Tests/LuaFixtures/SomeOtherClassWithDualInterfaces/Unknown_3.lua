-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMethodsTUnitTests.cs:646
-- @test: SomeOtherClassWithDualInterfaces.Unknown
-- @compat-notes: Uses injected variable: static
array = { { 1, 2, 3 }, { 11, 35, 77 }, { 16, 42, 64 }, {99, 76, 17 } };				
			
				x = static.SetComplexRecursive(array);

				return x;
