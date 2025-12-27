-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:216
-- @test: ClosureTUnitTests.NestedOutOfScopeUpValues
function X()
                    local x = 0;
                    local m = { };
                    function m:a()
                        self.t = {
                            dojob = function()
                                if (x == 0) then return 1; else return 0; end
                            end,
                        };
                    end
                    return m;
                end
                Q = X();
                Q:a();
                return 10 * Q.t.dojob();
