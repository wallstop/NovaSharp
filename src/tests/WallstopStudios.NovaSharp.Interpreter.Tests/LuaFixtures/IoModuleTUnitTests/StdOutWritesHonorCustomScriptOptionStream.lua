-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:1264
-- @test: IoModuleTUnitTests.StdOutWritesHonorCustomScriptOptionStream
-- Requires C# ScriptOptions stdout stream
io.write('brokered output'); io.flush()
