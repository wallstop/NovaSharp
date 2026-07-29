-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:293
-- @test: ArithmOperatorsTestClass.InteropMetaIterator
local sum = 0
                for i in o do
                    sum = sum + i
                end

                return sum;
