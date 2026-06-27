-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleVersionParityTUnitTests.cs:273
-- @test: LoadModuleVersionParityTUnitTests.LoadAcceptsNumberArgumentInLua52Plus
-- Test targets Lua 5.1
return load(123)
