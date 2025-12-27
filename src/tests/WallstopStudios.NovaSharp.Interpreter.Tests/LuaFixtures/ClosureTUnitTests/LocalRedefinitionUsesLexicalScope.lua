-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:240
-- @test: ClosureTUnitTests.LocalRedefinitionUsesLexicalScope
result = ''
                local hi = 'hello'
                local function test()
                    result = result .. hi;
                end
                test();
                hi = 'X'
                test();
                local hi = '!';
                test();
                return result;
