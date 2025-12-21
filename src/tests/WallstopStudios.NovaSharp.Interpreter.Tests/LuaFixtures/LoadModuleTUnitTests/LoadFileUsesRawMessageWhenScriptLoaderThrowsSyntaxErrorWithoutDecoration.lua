-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:369
-- @test: LoadModuleTUnitTests.LoadFileUsesRawMessageWhenScriptLoaderThrowsSyntaxErrorWithoutDecoration
-- @compat-notes: Test targets Lua 5.1
return loadfile('anything.lua')
