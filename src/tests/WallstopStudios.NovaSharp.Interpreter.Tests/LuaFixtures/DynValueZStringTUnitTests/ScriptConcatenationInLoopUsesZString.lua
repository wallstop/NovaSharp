-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\DataTypes\DynValueZStringTUnitTests.cs:184
-- @test: DynValueZStringTUnitTests.ScriptConcatenationInLoopUsesZString
-- @compat-notes: Lua 5.3+: bitwise operators
local s = ''
                for i = 1, 10 do
                    s = s .. i
                end
                return s
