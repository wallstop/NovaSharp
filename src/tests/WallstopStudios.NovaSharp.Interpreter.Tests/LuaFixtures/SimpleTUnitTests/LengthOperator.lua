-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:728
-- @test: SimpleTUnitTests.LengthOperator
x = 'ciao'
				y = { 1, 2, 3 }
   
				return #x, #y
