-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:305
-- @test: ErrorHandlingModuleTUnitTests.XpcallUsesOnlyTheFirstHandlerResult
local ok, first, extra = xpcall(function() error('boom', 0) end, function() return 1, 2 end)
                local ok2, value = xpcall(function() error('boom', 0) end, function() return nil end)
                local ok3, value3 = xpcall(function() error('boom', 0) end, function() end)

                print('multi', ok, first, extra)
                print('nil', ok2, value, type(value))
                print('none', ok3, value3, type(value3))

                return ok, first, extra, ok2, value, type(value), ok3, value3, type(value3)
