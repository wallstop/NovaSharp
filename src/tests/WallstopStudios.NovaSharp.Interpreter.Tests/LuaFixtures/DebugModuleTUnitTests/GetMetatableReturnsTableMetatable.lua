-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1878
-- @test: DebugModuleTUnitTests.GetMetatableReturnsTableMetatable
-- @compat-notes: Test targets Lua 5.1
local t = {}
                local mt = { __index = function() return 42 end }
                setmetatable(t, mt)
                local retrieved = debug.getmetatable(t)
                return retrieved == mt, t.anykey
