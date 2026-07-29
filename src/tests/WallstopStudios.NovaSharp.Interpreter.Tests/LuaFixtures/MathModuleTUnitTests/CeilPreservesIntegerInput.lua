-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:640
-- @test: MathModuleTUnitTests.CeilPreservesIntegerInput
-- Test targets Lua 5.3+
return math.ceil(42)
