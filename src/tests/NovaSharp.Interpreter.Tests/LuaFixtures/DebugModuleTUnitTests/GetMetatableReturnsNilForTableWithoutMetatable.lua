-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1195
-- @test: DebugModuleTUnitTests.GetMetatableReturnsNilForTableWithoutMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                return debug.getmetatable(t)
