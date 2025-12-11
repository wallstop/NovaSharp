-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:1224
-- @test: DebugModuleTUnitTests.SetMetatableOnTableWorks
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                local mt = { __index = function() return 'found' end }
                debug.setmetatable(t, mt)
                return t.missing
