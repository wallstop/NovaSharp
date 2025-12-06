-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:86
-- @test: DebugModuleTUnitTests.GetLocalExposesCurrentLevelArguments
-- @compat-notes: Lua 5.3+: bitwise operators
local name, value = debug.getlocal(0, 1)
                return type(name), value
