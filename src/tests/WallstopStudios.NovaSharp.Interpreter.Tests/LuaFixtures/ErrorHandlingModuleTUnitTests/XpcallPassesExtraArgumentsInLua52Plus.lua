-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:551
-- @test: ErrorHandlingModuleTUnitTests.XpcallPassesExtraArgumentsInLua52Plus
-- @compat-notes: Test targets Lua 5.2+
local received = {}
                local ok, a, b, c = xpcall(function(...) 
                    for i, v in ipairs({...}) do received[i] = v end
                    return ...
                end, function() end, 1, 2, 3)
                return ok, #received, a, b, c
