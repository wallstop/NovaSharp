-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:164
-- @test: ClosureTUnitTests.ClosureWithoutTableReuse
x = 0
                function container()
                    local x = 20
                    for i=1,5 do
                        local y = 0
                        function zz() y=y+1; x = x * 10; return x+y end
                        a1 = a2; a2 = a3; a3 = a4; a4 = a5; a5 = zz;
                    end
                end
                container();
                x = 4000
                return a1(), a2(), a3(), a4(), a5()
