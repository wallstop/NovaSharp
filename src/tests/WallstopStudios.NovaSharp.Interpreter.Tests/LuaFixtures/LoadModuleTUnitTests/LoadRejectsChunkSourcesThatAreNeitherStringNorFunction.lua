-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:260
-- @test: LoadModuleTUnitTests.LoadRejectsChunkSourcesThatAreNeitherStringNorFunction
-- @compat-notes: Test targets Lua 5.1
load(true)
