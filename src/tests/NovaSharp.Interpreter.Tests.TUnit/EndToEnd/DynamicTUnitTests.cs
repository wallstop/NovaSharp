#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class DynamicTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DynamicAccessEval()
        {
            DynValue result = Script.RunString("return dynamic.eval('5+1');");
            await EndToEndDynValueAssert.ExpectAsync(result, 6);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicAccessPrepare()
        {
            string code =
                @"
                local prepared = dynamic.prepare('5+1');
                return dynamic.eval(prepared);
                ";
            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 6);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicAccessScope()
        {
            string code =
                @"
                a = 3;
                local prepared = dynamic.prepare('a+1');
                function worker()
                    a = 5;
                    return dynamic.eval(prepared);
                end
                return worker();
                ";

            DynValue result = Script.RunString(code);
            await EndToEndDynValueAssert.ExpectAsync(result, 6);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicAccessScopeSecurityReturnsNil()
        {
            string code =
                @"
                a = 5;
                local prepared = dynamic.prepare('a');
                local eval = dynamic.eval;
                local _ENV = { }
                function worker()
                    return eval(prepared);
                end
                return worker();
                ";

            DynValue result = Script.RunString(code);
            await Assert.That(result.Type).IsEqualTo(DataType.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task DynamicAccessFromCSharp()
        {
            Script script = new();
            script.DoString("t = { ciao = { 'hello' } }");

            DynValue evaluation = script
                .CreateDynamicExpression("t.ciao[1] .. ' world'")
                .Evaluate();
            await Assert.That(evaluation.String).IsEqualTo("hello world");
        }
    }
}
#pragma warning restore CA2007
