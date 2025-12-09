-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:524
-- @test: DebugModuleTUnitTests.GetInfoReturnsLuaFunctionPlaceholderWithFFlag
-- @compat-notes: Lua 5.3+: bitwise operators
local function sample() end
                local info = debug.getinfo(sample, 'f')
                return info.func
