-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:218
-- @test: MathModuleTUnitTests.ToIntegerReturnsNilForNil
-- @compat-notes: Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(nil)
