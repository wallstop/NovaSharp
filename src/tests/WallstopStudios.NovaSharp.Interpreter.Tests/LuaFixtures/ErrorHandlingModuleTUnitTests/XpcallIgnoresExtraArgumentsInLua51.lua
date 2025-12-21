-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:513
-- @test: ErrorHandlingModuleTUnitTests.XpcallIgnoresExtraArgumentsInLua51
-- @compat-notes: Test targets Lua 5.1
local receivedWithExtras = {}
                local receivedWithoutExtras = {}
                
                -- Call with extra args
                xpcall(function(...) 
                    for i, v in ipairs({...}) do receivedWithExtras[i] = v end
                end, function() end, 1, 2, 3)
                
                -- Call without extra args
                xpcall(function(...) 
                    for i, v in ipairs({...}) do receivedWithoutExtras[i] = v end
                end, function() end)
                
                -- In Lua 5.1, both should receive the same number of args
                return #receivedWithExtras, #receivedWithoutExtras
