-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:583
-- @test: ErrorHandlingModuleTUnitTests.XpcallExtraArgsAvailableBeforeErrorInLua52Plus
-- @compat-notes: Test targets Lua 5.2+
local captured = nil
                local ok, err = xpcall(function(a, b, c) 
                    captured = a + b + c
                    error('intentional error')
                end, function(e) return 'handled: ' .. e end, 10, 20, 30)
                return ok, captured
