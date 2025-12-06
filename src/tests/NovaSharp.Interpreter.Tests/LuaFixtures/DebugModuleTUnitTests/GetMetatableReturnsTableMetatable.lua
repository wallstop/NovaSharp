-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1174
-- @test: DebugModuleTUnitTests.GetMetatableReturnsTableMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                local mt = { __index = function() return 42 end }
                setmetatable(t, mt)
                local retrieved = debug.getmetatable(t)
                return retrieved == mt, t.anykey
