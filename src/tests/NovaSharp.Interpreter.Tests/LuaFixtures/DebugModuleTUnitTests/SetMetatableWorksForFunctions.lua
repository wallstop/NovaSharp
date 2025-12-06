-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:375
-- @test: DebugModuleTUnitTests.SetMetatableWorksForFunctions
-- @compat-notes: Lua 5.3+: bitwise operators
local f = function() return 42 end
                local mt = { __call = function() return 'called' end }
                local success = debug.setmetatable(f, mt) ~= nil
                return success
