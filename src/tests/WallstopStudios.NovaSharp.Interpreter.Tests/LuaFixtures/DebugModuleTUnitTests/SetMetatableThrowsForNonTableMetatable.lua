-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:357
-- @test: DebugModuleTUnitTests.SetMetatableThrowsForNonTableMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function() debug.setmetatable({}, 'notatable') end)
                return ok, err
