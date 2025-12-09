-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:344
-- @test: MathModuleTUnitTests.FloorReturnsIntegerTypeReportedByMathType
-- @compat-notes: Lua 5.3+: math.type (5.3+)
return math.type(math.floor(3.7))
