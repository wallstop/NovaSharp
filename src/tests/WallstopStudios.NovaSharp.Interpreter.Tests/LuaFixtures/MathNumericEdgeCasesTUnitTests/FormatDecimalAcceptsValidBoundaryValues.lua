-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:903
-- @test: MathNumericEdgeCasesTUnitTests.FormatDecimalAcceptsValidBoundaryValues
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.3+
return string.format('%d', {luaExpression})
