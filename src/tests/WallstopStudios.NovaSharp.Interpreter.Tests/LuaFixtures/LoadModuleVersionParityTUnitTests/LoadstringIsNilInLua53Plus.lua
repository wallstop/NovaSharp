-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleVersionParityTUnitTests.cs:52
-- @test: LoadModuleVersionParityTUnitTests.LoadstringIsNilInLua53Plus
-- Test targets Lua 5.1
return type(loadstring)
