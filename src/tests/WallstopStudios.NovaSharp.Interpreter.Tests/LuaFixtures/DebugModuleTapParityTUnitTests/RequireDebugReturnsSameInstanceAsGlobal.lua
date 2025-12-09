-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:44
-- @test: DebugModuleTapParityTUnitTests.RequireDebugReturnsSameInstanceAsGlobal
-- @compat-notes: Lua 5.3+: bitwise operators
local first = require('debug')
                local second = require('debug')
                return first == debug, first == second
