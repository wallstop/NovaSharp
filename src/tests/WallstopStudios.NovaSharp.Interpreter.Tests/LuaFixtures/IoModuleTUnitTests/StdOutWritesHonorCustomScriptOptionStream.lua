-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1264
-- @test: IoModuleTUnitTests.StdOutWritesHonorCustomScriptOptionStream
-- Compatibility notes: Test method 'StdOutWritesHonorCustomScriptOptionStream' tests NovaSharp-specific behavior (StdOutWritesHonorCustomScriptOptionStream)
io.write('brokered output'); io.flush()
