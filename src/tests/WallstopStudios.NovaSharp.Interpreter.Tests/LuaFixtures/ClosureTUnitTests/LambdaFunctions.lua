-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:45
-- @test: ClosureTUnitTests.LambdaFunctions
-- @compat-notes: NovaSharp: metalua-style lambda syntax
g = |f, x|f(x, x+1)
                f = |x, y, z|x*(y+z)
                return g(|x,y|f(x,y,1), 2)
