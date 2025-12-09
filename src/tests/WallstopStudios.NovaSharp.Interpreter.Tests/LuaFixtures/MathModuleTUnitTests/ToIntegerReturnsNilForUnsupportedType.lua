-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:193
-- @test: MathModuleTUnitTests.ToIntegerReturnsNilForUnsupportedType
-- @compat-notes: Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(true)
