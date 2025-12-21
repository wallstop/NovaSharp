-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1599
-- @test: DebugModuleTUnitTests.SetMetatableOnBooleanSetsTypeMetatable
-- @compat-notes: Test targets Lua 5.1
local mt = { __tostring = function(v) return 'custom_bool' end }
                debug.setmetatable(true, mt)
                return true
