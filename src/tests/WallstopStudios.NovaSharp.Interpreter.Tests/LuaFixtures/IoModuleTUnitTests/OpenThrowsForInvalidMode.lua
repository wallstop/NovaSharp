-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:83
-- @test: IoModuleTUnitTests.OpenThrowsForInvalidMode
-- @compat-notes: Lua 5.1 returns (nil, error_message) for invalid modes. Windows Lua 5.1 crashes (STATUS_STACK_BUFFER_OVERRUN) on this - that's a bug in reference Lua.
return io.open('{path}', 'z')