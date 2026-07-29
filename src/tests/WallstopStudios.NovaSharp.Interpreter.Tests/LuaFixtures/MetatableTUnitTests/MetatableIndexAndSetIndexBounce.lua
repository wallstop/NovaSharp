-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:182
-- @test: MetatableTUnitTests.MetatableIndexAndSetIndexBounce
T = { a = 'a', b = 'b', c = 'c' };
                t = { };
                m = { __index = T, __newindex = T };
                s = '';
                setmetatable(t, m);
                s = s .. t.a .. t.b .. t.c;
                t.a = '!';
                s = s .. t.a .. t.b .. t.c;
                return s;
