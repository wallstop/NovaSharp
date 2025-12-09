-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:466
-- @test: DebugModuleTUnitTests.GetInfoReturnsClrInfoForCallbackFunctions
-- @compat-notes: Lua 5.3+: bitwise operators
local info = debug.getinfo(print)
                return info.what, info.source
