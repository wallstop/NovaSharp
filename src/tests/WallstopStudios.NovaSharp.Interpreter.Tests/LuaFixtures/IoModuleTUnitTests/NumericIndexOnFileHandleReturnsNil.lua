-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:282
-- @test: IoModuleTUnitTests.NumericIndexOnFileHandleReturnsNil
-- @compat-notes: Test targets Lua 5.1
return io.stdin[1]
