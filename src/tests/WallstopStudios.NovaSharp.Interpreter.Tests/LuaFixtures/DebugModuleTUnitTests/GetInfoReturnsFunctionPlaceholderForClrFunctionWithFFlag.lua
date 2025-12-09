-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:507
-- @test: DebugModuleTUnitTests.GetInfoReturnsFunctionPlaceholderForClrFunctionWithFFlag
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: func
local info = debug.getinfo(print, 'f')
                return info.func
