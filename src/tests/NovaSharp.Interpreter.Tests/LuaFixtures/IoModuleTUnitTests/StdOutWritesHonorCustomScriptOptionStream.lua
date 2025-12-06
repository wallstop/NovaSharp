-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:905
-- @test: IoModuleTUnitTests.StdOutWritesHonorCustomScriptOptionStream
io.write('brokered output'); io.flush()
