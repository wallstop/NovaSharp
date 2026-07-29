-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleVersionParityTUnitTests.cs:42
-- @test: LoadModuleVersionParityTUnitTests.LoadstringIsDeprecatedButAvailableInLua52
-- Test targets Lua 5.1
return type(loadstring)
