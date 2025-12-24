-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:458
-- @test: LoadModuleTUnitTests.DoFileWrapsSyntaxErrorsWithScriptRuntimeException
-- @compat-notes: Test targets Lua 5.1
dofile('broken.lua')
