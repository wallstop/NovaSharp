-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1936
-- @test: BasicModuleTUnitTests.AssertPreservesLuaErrorValues
local original = {}
                local ok, e = pcall(function() assert(false, original) end)
                local ok2, e2 = pcall(function() assert(false) end)
                local ok3, e3 = pcall(function() assert(false, 42) end)
                local ok4, e4 = pcall(function() assert(false, nil) end)

                print('table', ok, type(e), rawequal(original, e),
                    type(e) == 'string' and e:find('string expected') ~= nil)
                print('default', ok2, type(e2), tostring(e2):find('assertion failed!') ~= nil)
                print('number', ok3, type(e3), type(e3) == 'number' and e3 or (tostring(e3):find('42') ~= nil))
                print('nil', ok4, type(e4), e4 == nil,
                    type(e4) == 'string' and e4:find('assertion failed!') ~= nil)

                return
                    ok, type(e), rawequal(original, e),
                    type(e) == 'string' and e:find('string expected') ~= nil,
                    ok2, type(e2), tostring(e2):find('assertion failed!') ~= nil,
                    ok3, type(e3), type(e3) == 'number' and e3 or tostring(e3):find('42') ~= nil,
                    ok4, type(e4), e4 == nil, tostring(e4)
