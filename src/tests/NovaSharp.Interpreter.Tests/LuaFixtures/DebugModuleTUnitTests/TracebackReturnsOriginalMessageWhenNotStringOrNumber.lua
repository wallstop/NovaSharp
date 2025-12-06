-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:995
-- @test: DebugModuleTUnitTests.TracebackReturnsOriginalMessageWhenNotStringOrNumber
-- @compat-notes: Lua 5.3+: bitwise operators
local t = { custom = 'value' }
                return debug.traceback(t)
