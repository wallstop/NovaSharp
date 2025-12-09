-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ColonOperatorBehaviourTUnitTests.cs:36
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsDotKeepsMethodCallForCallbacks
return target:invoke(123)
