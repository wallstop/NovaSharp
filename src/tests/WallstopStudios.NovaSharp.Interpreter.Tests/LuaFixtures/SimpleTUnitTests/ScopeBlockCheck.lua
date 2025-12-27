-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:599
-- @test: SimpleTUnitTests.ScopeBlockCheck
local x = 6;
				
				do
					local i = 33;
				end
		
				return i, x
