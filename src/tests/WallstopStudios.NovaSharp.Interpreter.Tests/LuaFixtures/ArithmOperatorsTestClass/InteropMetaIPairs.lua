-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/UserDataMetaTUnitTests.cs:269
-- @test: ArithmOperatorsTestClass.InteropMetaIPairs
local str = ''
                for k,v in ipairs(o) do
                    str = str .. k .. v;
                end

                return str;
