-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:186
-- @test: ErrorHandlingModuleTUnitTests.PcallPreservesLuaErrorValues
local original = {}
                local ok1, e1 = pcall(function() error(original) end)
                local ok2, e2 = pcall(function() error(42) end)
                local ok3, e3 = pcall(function() error(nil) end)
                local ok4, e4 = pcall(function() error(false) end)
                print('table', ok1, rawequal(original, e1), type(e1))
                print('number', ok2, type(e2), type(e2) == 'number' and e2 or (tostring(e2):find('42') ~= nil))
                print('nil', ok3, type(e3), e3)
                print('false', ok4, type(e4), e4)
                return
                    ok1, rawequal(original, e1), type(e1),
                    ok2, type(e2), type(e2) == 'number' and e2 or tostring(e2):find('42') ~= nil,
                    ok3, type(e3), e3,
                    ok4, type(e4), e4
