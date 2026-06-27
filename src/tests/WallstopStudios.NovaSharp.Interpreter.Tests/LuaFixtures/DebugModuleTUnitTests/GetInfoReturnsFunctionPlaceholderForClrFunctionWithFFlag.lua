-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:1034
-- @test: DebugModuleTUnitTests.GetInfoReturnsFunctionPlaceholderForClrFunctionWithFFlag
-- Test targets Lua 5.1; Uses injected variable: func
local info = debug.getinfo(print, 'f')
                return info.func
