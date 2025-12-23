-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ColonOperatorBehaviourTUnitTests.cs:39
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsDotKeepsMethodCallForCallbacks
return target:invoke(123)
