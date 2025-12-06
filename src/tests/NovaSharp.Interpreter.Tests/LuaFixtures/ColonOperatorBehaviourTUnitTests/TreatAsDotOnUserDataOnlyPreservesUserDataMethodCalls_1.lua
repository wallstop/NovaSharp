-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ColonOperatorBehaviourTUnitTests.cs:100
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsDotOnUserDataOnlyPreservesUserDataMethodCalls
return userTarget:Invoke(456)
