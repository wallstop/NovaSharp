-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1218
-- @test: SimpleTUnitTests.LoopWithReturn
function Allowed( )
									for i = 1, 20 do
  										return false 
									end
									return true
								end
						Allowed();
