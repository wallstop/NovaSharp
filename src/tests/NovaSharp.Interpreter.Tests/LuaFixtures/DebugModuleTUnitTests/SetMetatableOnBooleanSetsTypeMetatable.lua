-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1132
-- @test: DebugModuleTUnitTests.SetMetatableOnBooleanSetsTypeMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
local mt = { __tostring = function(v) return 'custom_bool' end }
                debug.setmetatable(true, mt)
                return true
