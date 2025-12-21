-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:44
-- @test: LoadModuleVersionParityTUnitTests.LoadstringIsNilInLua52Plus
-- @compat-notes: Test targets Lua 5.1
return type(loadstring)
