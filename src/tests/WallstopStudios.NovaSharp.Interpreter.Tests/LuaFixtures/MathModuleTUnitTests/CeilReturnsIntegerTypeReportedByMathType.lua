-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:653
-- @test: MathModuleTUnitTests.CeilReturnsIntegerTypeReportedByMathType
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+)
return math.type(math.ceil(3.2))
