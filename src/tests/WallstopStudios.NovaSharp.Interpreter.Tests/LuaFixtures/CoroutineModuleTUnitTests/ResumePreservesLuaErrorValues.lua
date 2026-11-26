-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:122
-- @test: CoroutineModuleTUnitTests.ResumePreservesLuaErrorValues
local original = {}
                local co = coroutine.create(function() error(original) end)
                local ok, e = coroutine.resume(co)

                local co2 = coroutine.create(function() error('boom') end)
                local ok2, e2 = coroutine.resume(co2)

                local co3 = coroutine.create(function() error(nil) end)
                local ok3, e3 = coroutine.resume(co3)

                print('table', ok, rawequal(original, e), type(e))
                print('string', ok2, type(e2), e2:find('boom') ~= nil)
                print('nil', ok3, type(e3), e3)

                return
                    ok, rawequal(original, e), type(e),
                    ok2, type(e2), e2:find('boom') ~= nil,
                    ok3, type(e3), e3
