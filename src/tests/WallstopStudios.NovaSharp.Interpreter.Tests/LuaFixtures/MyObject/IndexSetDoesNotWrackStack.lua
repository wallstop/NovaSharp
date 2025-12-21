-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:256
-- @test: MyObject.IndexSetDoesNotWrackStack
local aClass = {}
                setmetatable(aClass, {__newindex = function() end, __index = function() end })
                local p = {a = 1, b = 2}
                for x , v in pairs(p) do
                    print (x, v)
                    aClass[x] = v
                end
