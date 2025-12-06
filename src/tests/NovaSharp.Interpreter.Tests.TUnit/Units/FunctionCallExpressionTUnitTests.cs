namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;

    public sealed class FunctionCallExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CompileEmitsCallWithParenthesizedArguments()
        {
            FunctionCallExpression expression = CreateExpression("(1, \"two\")", out Script script);
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            Instruction call = byteCode.Code[^1];

            await Assert.That(call.OpCode).IsEqualTo(OpCode.Call);
            await Assert.That(call.NumVal).IsEqualTo(2);
            await Assert.That(call.Name).IsEqualTo("stub::callee");
            bool containsNumberLiteral = byteCode.Code.Any(instruction =>
                instruction.OpCode == OpCode.Literal && instruction.Value.Number == 1
            );
            await Assert.That(containsNumberLiteral).IsTrue();
            bool containsStringLiteral = byteCode.Code.Any(instruction =>
                instruction.OpCode == OpCode.Literal && instruction.Value.String == "two"
            );
            await Assert.That(containsStringLiteral).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CompileEmitsThisCallForColonSyntax()
        {
            FunctionCallExpression expression = CreateExpression(
                "(42)",
                out Script script,
                "withColon"
            );
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            int copyIndex = byteCode.Code.FindIndex(instruction =>
                instruction.OpCode == OpCode.Copy
            );
            int indexIndex = byteCode.Code.FindIndex(instruction =>
                instruction.OpCode == OpCode.IndexN
            );
            int swapIndex = byteCode.Code.FindIndex(instruction =>
                instruction.OpCode == OpCode.Swap
            );
            Instruction thisCall = byteCode.Code[^1];

            await Assert.That(copyIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(indexIndex).IsEqualTo(copyIndex + 1);
            await Assert.That(swapIndex).IsEqualTo(indexIndex + 1);

            Instruction copy = byteCode.Code[copyIndex];
            Instruction index = byteCode.Code[indexIndex];
            Instruction swap = byteCode.Code[swapIndex];

            await Assert.That(copy.NumVal).IsEqualTo(0);
            await Assert.That(index.Value.String).IsEqualTo("withColon");
            await Assert.That(swap.NumVal).IsEqualTo(0);
            await Assert.That(swap.NumVal2).IsEqualTo(1);
            await Assert.That(thisCall.OpCode).IsEqualTo(OpCode.ThisCall);
            await Assert.That(thisCall.NumVal).IsEqualTo(2);
            await Assert.That(thisCall.Name).IsEqualTo("stub::callee");
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorAllowsStringLiteralArguments()
        {
            FunctionCallExpression expression = CreateExpression("\"payload\"", out Script script);
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            Instruction call = byteCode.Code[^1];

            await Assert.That(call.OpCode).IsEqualTo(OpCode.Call);
            await Assert.That(call.NumVal).IsEqualTo(1);
            bool containsStringLiteral = byteCode.Code.Any(instruction =>
                instruction.OpCode == OpCode.Literal && instruction.Value.String == "payload"
            );
            await Assert.That(containsStringLiteral).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorAllowsTableConstructorArguments()
        {
            FunctionCallExpression expression = CreateExpression(
                "{ value = 1 }",
                out Script script
            );
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            Instruction call = byteCode.Code[^1];

            await Assert.That(call.OpCode).IsEqualTo(OpCode.Call);
            await Assert.That(call.NumVal).IsEqualTo(1);
        }

        [global::TUnit.Core.Test]
        public async Task ConstructorThrowsWhenArgumentsMissing()
        {
            Script script = new();
            ScriptLoadingContext context = new(script) { Lexer = new Lexer(0, string.Empty, true) };
            Expression callee = new FunctionExpressionStub(context, "broken::callee");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
            {
                _ = new FunctionCallExpression(context, callee, null);
            })!;

            await Assert.That(exception.Message).Contains("function arguments expected");
            await Assert.That(exception.IsPrematureStreamTermination).IsTrue();
        }

        private static FunctionCallExpression CreateExpression(
            string argumentSource,
            out Script script,
            string methodName = null
        )
        {
            script = new Script();
            ScriptLoadingContext context = new(script)
            {
                Lexer = new Lexer(0, argumentSource, true),
            };
            Expression callee = new FunctionExpressionStub(context, "stub::callee");
            Token methodToken = methodName == null ? null : CreateToken(TokenType.Name, methodName);
            return new FunctionCallExpression(context, callee, methodToken);
        }

        private static Token CreateToken(TokenType type, string text)
        {
            return new Token(type, 0, 0, 0, 0, 0, 0, 0) { Text = text };
        }

        private sealed class FunctionExpressionStub : Expression
        {
            private readonly string _friendlyName;

            public FunctionExpressionStub(ScriptLoadingContext context, string friendlyName)
                : base(context)
            {
                _friendlyName = friendlyName;
            }

            public override void Compile(ByteCode bc)
            {
                bc.EmitLiteral(DynValue.NewString("function-stub"));
            }

            public override string GetFriendlyDebugName()
            {
                return _friendlyName;
            }

            public override DynValue Eval(ScriptExecutionContext context)
            {
                return DynValue.Nil;
            }
        }
    }
}
