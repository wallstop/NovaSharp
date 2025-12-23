-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ColonOperatorBehaviourTUnitTests.cs:67
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsColonDisablesMethodCallFlag
return target:invoke(123)
