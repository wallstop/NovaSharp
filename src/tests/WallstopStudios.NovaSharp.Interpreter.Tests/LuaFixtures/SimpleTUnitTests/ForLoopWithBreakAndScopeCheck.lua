-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:757
-- @test: SimpleTUnitTests.ForLoopWithBreakAndScopeCheck
x = 0

				for i = 1, 10 do
					x = x + i

					if (i == 3) then
						break
					end
				end
    
				return i, x
