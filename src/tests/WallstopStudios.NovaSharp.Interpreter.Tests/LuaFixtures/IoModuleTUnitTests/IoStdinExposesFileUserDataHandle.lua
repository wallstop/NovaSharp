-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:267
-- @test: IoModuleTUnitTests.IoStdinExposesFileUserDataHandle
-- @compat-notes: Test targets Lua 5.1
return io.stdin
