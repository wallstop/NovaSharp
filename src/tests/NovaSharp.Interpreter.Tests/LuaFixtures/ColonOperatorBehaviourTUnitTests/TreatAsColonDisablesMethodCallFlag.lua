-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ColonOperatorBehaviourTUnitTests.cs:63
-- @test: ColonOperatorBehaviourTUnitTests.TreatAsColonDisablesMethodCallFlag
return target:invoke(123)
