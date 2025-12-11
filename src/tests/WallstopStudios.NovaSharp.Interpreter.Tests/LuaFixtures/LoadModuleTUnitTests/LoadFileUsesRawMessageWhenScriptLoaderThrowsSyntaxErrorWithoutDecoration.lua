-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleTUnitTests.cs:252
-- @test: LoadModuleTUnitTests.LoadFileUsesRawMessageWhenScriptLoaderThrowsSyntaxErrorWithoutDecoration
return loadfile('anything.lua')
