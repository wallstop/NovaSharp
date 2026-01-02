-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\StringPackModuleTUnitTests.cs:237
-- @test: StringPackModuleTUnitTests.PackSizeThrowsOnVariableLength
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.packsize (5.3+)
return string.packsize('z')
