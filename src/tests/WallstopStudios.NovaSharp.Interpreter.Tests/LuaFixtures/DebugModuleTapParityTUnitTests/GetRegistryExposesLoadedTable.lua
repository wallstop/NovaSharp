-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:111
-- @test: DebugModuleTapParityTUnitTests.GetRegistryExposesLoadedTable
-- @compat-notes: Lua 5.3+: bitwise operators
local debug = require('debug')
                return debug.getregistry()
