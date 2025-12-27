-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2339
-- @test: DebugModuleTUnitTests.UpvalueIdDataDrivenClrFunctionBehavior
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return debug.upvalueid({clrFunctionName}, 1)
