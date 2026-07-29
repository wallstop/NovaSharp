-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ColonOperatorBehaviourTUnitTests.cs:125
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsColonDisablesMethodCallFlagForCallbackViews
return target:invoke(123)
