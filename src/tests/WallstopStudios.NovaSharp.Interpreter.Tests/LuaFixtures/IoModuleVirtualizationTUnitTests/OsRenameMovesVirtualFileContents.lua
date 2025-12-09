-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:115
-- @test: IoModuleVirtualizationTUnitTests.OsRenameMovesVirtualFileContents
-- @compat-notes: Lua 5.3+: bitwise operators
local f = io.open('old.txt', 'w'); f:write('payload'); f:close()
