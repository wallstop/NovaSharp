-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/IntegerBoundaryTUnitTests.cs:327
-- @test: IntegerBoundaryTUnitTests.MathTointegerStringArguments
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.3+
return math.tointeger({expression})
