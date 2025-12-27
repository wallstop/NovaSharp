-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:87
-- @test: ClosureTUnitTests.ClosuresCaptureIndependently
a = {}
                x = 0
                function container()
                    local x = 20
                    for i=1,5 do
                        local y = 0
                        a[i] = function () y=y+1; x = x * 10; return x+y end
                    end
                end
                container();
                x = 4000
                return a[1](), a[2](), a[3](), a[4](), a[5]()
