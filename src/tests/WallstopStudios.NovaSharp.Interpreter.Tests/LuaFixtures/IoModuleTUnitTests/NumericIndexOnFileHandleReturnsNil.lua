-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:282
-- @test: IoModuleTUnitTests.NumericIndexOnFileHandleReturnsNil
-- Test targets Lua 5.1
return io.stdin[1]
