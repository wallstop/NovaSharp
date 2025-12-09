-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:27
-- @test: IoModuleVirtualizationTUnitTests.IoOpenWriteThenReadUsesVirtualFileSystem
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.open('virtual.txt', 'w'); f:write('hello'); f:close()
