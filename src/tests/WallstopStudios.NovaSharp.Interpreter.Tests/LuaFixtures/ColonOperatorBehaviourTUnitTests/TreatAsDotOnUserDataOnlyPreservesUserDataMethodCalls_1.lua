-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ColonOperatorBehaviourTUnitTests.cs:107
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsDotOnUserDataOnlyPreservesUserDataMethodCalls
return userTarget:Invoke(456)
