-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:758
-- @test: IoModuleTUnitTests.TypeReturnsNilForNonUserDataArguments
-- Test targets Lua 5.1
return io.type(42), io.type({})
