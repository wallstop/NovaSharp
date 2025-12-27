-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1055
-- @test: SimpleTUnitTests.SimpleFunc
function fact (n)
					return 3;
				end
    
				return fact(3)
