-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:119
-- @test: DebugModuleTapParityTUnitTests.GetRegistryExposesLoadedTable
local debug = require('debug')
                return debug.getregistry()
