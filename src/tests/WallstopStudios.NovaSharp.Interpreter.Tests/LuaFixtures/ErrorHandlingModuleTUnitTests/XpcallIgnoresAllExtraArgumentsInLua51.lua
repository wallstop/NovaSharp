-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:610
-- @test: ErrorHandlingModuleTUnitTests.XpcallIgnoresAllExtraArgumentsInLua51
-- @compat-notes: Test targets Lua 5.1
local countWithExtras = 0
                local countWithoutExtras = 0
                
                -- Call with many extra args
                xpcall(function(...) 
                    countWithExtras = select('#', ...)
                end, function() end, 'a', 'b', 'c', 'd', 'e', 1, 2, 3, 4, 5)
                
                -- Call without extra args  
                xpcall(function(...) 
                    countWithoutExtras = select('#', ...)
                end, function() end)
                
                -- In Lua 5.1, both should receive the same number of args
                return countWithExtras, countWithoutExtras
