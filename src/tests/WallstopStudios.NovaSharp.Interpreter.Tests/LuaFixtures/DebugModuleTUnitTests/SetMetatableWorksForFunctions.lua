-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:638
-- @test: DebugModuleTUnitTests.SetMetatableWorksForFunctions
-- @compat-notes: Test targets Lua 5.1
local f = function() return 42 end
                local mt = { __call = function() return 'called' end }
                local success = debug.setmetatable(f, mt) ~= nil
                return success
