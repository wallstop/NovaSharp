-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:944
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberHandlesExponentWithSignAndBuffersRemainder
-- @compat-notes: Lua 5.3+: bitwise operators
local n = file:read('*n'); return n, file:read('*l'), file:read('*l')
