-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StreamFileUserDataBaseTUnitTests.cs:944
-- @test: StreamFileUserDataBaseTUnitTests.ReadNumberHandlesExponentWithSignAndBuffersRemainder
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: file
local n = file:read('*n'); return n, file:read('*l'), file:read('*l')
