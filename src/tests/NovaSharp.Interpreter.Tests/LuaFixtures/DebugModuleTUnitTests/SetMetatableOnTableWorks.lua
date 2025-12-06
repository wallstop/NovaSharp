-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1211
-- @test: DebugModuleTUnitTests.SetMetatableOnTableWorks
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                local mt = { __index = function() return 'found' end }
                debug.setmetatable(t, mt)
                return t.missing
