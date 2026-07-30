-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1080
-- @test: SimpleTUnitTests.IterativeFactorialWithFor
-- defines a factorial function
				function fact (n)
					x = 1
					for i = n, 1, -1 do
						x = x * i;
					end

					return x;
				end
    
				return fact(5)
