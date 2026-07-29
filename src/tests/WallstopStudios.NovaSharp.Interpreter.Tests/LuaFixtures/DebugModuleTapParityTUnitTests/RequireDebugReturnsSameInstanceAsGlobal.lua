-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:48
-- @test: DebugModuleTapParityTUnitTests.RequireDebugReturnsSameInstanceAsGlobal
local first = require('debug')
                local second = require('debug')
                return first == debug, first == second
