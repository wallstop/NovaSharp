-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMethodsTUnitTests.cs:584
-- @test: SomeOtherClassWithDualInterfaces.Unknown
-- @compat-notes: Uses injected variable: static
strlist = { 'ciao', 'hello', 'aloha' };
				intlist = {  };
				dictry = { ciao = 39, hello = 78, aloha = 128 };
				
				x = static.SetComplexTypes(strlist, intlist, dictry, strlist, intlist);

				return x;
