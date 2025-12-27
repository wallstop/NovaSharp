-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/DynValueZStringTUnitTests.cs:191
-- @test: DynValueZStringTUnitTests.ScriptConcatenationInLoopUsesZString
local s = ''
                for i = 1, 10 do
                    s = s .. i
                end
                return s
