-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:78
-- @test: IoModuleTUnitTests.RegisteredFileCallbacksUseArgumentViews
return io.stderr.close,
       io.stderr.flush,
       io.stderr.lines,
       io.stderr.read,
       io.stderr.seek,
       io.stderr.setvbuf,
       io.stderr.write
