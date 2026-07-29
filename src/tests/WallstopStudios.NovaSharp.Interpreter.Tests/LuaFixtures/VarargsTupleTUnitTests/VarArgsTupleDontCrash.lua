-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/VarargsTupleTUnitTests.cs:66
-- @test: VarargsTupleTUnitTests.VarArgsTupleDontCrash
function Obj(...)
                    local args = { ... }
                end
                Obj(1)
