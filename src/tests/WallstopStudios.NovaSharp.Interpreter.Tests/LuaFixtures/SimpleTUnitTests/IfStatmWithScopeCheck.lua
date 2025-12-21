-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:575
-- @test: SimpleTUnitTests.IfStatmWithScopeCheck
x = 0

				if (x == 0) then
					local i = 3;
					x = i * 2;
				end
    
				return i, x
