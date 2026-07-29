-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:624
-- @test: SimpleTUnitTests.ForLoopWithBreak
x = 0

				for i = 1, 10 do
					x = i
					break;
				end
    
				return x
