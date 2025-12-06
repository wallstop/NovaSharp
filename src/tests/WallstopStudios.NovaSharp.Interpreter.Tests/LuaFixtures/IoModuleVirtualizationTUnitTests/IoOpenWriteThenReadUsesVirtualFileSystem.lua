-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:27
-- @test: IoModuleVirtualizationTUnitTests.IoOpenWriteThenReadUsesVirtualFileSystem
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.open('virtual.txt', 'w'); f:write('hello'); f:close()
