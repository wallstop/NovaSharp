-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:245
-- @test: ArithmOperatorsTestClass.InteropMetaPairs
local str = ''
                for k,v in pairs(o) do
                    str = str .. k .. v;
                end

                return str;
