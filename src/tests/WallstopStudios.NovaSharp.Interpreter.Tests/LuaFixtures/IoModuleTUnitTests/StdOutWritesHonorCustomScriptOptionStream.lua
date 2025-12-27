-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1264
-- @test: IoModuleTUnitTests.StdOutWritesHonorCustomScriptOptionStream
-- @compat-notes: Test targets Lua 5.1
io.write('brokered output'); io.flush()
