-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:295
-- @test: LoadModuleTUnitTests.DoFileExecutesLoadedChunk
return dofile('script.lua')
