-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ColonOperatorBehaviourTUnitTests.cs:94
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsDotOnUserDataOnlyPreservesUserDataMethodCalls
return tableTarget:invoke(123)
