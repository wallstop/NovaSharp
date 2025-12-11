-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTapParityTUnitTests.cs:144
-- @test: DebugModuleTapParityTUnitTests.SetMetatableErrorMatchesLuaFormat
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    debug.setmetatable({}, true)
                end)
                return ok, err
