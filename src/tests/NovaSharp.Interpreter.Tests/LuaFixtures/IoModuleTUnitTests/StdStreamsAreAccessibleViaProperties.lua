-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:361
-- @test: IoModuleTUnitTests.StdStreamsAreAccessibleViaProperties
-- @compat-notes: Lua 5.3+: bitwise operators
return io.stdin ~= nil, io.stdout ~= nil, io.stderr ~= nil, io.unknown == nil
