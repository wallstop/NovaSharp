-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1152
-- @test: DebugModuleTUnitTests.SetMetatableThrowsForUserDataWithoutDescriptor
-- @compat-notes: Lua 5.3+: bitwise operators
local co = coroutine.create(function() end)
                local ok, err = pcall(function() debug.setmetatable(co, {}) end)
                return ok, err
