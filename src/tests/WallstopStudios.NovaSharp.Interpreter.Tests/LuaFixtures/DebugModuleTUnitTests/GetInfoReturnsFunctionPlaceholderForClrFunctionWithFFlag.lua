-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:507
-- @test: DebugModuleTUnitTests.GetInfoReturnsFunctionPlaceholderForClrFunctionWithFFlag
-- @compat-notes: Lua 5.3+: bitwise operators
local info = debug.getinfo(print, 'f')
                return info.func
