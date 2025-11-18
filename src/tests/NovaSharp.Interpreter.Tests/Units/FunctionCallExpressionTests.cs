namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Linq;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class FunctionCallExpressionTests
    {
        [Test]
        public void CompileEmitsCallWithParenthesizedArguments()
        {
            FunctionCallExpression expression = CreateExpression("(1, \"two\")", out Script script);
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            Instruction call = byteCode.code[^1];

            Assert.Multiple(() =>
            {
                Assert.That(call.OpCode, Is.EqualTo(OpCode.Call));
                Assert.That(call.NumVal, Is.EqualTo(2));
                Assert.That(call.Name, Is.EqualTo("stub::callee"));
                Assert.That(
                    byteCode.code.Any(instruction =>
                        instruction.OpCode == OpCode.Literal && instruction.Value.Number == 1
                    ),
                    Is.True
                );
                Assert.That(
                    byteCode.code.Any(instruction =>
                        instruction.OpCode == OpCode.Literal && instruction.Value.String == "two"
                    ),
                    Is.True
                );
            });
        }

        [Test]
        public void CompileEmitsThisCallForColonSyntax()
        {
            FunctionCallExpression expression = CreateExpression(
                "(42)",
                out Script script,
                "withColon"
            );
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            int copyIndex = byteCode.code.FindIndex(instruction =>
                instruction.OpCode == OpCode.Copy
            );
            int indexIndex = byteCode.code.FindIndex(instruction =>
                instruction.OpCode == OpCode.IndexN
            );
            int swapIndex = byteCode.code.FindIndex(instruction =>
                instruction.OpCode == OpCode.Swap
            );
            Instruction thisCall = byteCode.code[^1];

            Assert.Multiple(() =>
            {
                Assert.That(copyIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(indexIndex, Is.EqualTo(copyIndex + 1));
                Assert.That(swapIndex, Is.EqualTo(indexIndex + 1));

                Instruction copy = byteCode.code[copyIndex];
                Instruction index = byteCode.code[indexIndex];
                Instruction swap = byteCode.code[swapIndex];

                Assert.That(copy.NumVal, Is.EqualTo(0));
                Assert.That(index.Value.String, Is.EqualTo("withColon"));
                Assert.That(swap.NumVal, Is.EqualTo(0));
                Assert.That(swap.NumVal2, Is.EqualTo(1));

                Assert.That(thisCall.OpCode, Is.EqualTo(OpCode.ThisCall));
                Assert.That(
                    thisCall.NumVal,
                    Is.EqualTo(2),
                    "self argument should increment arg count"
                );
                Assert.That(thisCall.Name, Is.EqualTo("stub::callee"));
            });
        }

        [Test]
        public void ConstructorAllowsStringLiteralArguments()
        {
            FunctionCallExpression expression = CreateExpression("\"payload\"", out Script script);
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            Instruction call = byteCode.code[^1];

            Assert.Multiple(() =>
            {
                Assert.That(call.OpCode, Is.EqualTo(OpCode.Call));
                Assert.That(call.NumVal, Is.EqualTo(1));
                Assert.That(
                    byteCode.code.Any(instruction =>
                        instruction.OpCode == OpCode.Literal
                        && instruction.Value.String == "payload"
                    ),
                    Is.True
                );
            });
        }

        [Test]
        public void ConstructorAllowsTableConstructorArguments()
        {
            FunctionCallExpression expression = CreateExpression(
                "{ value = 1 }",
                out Script script
            );
            ByteCode byteCode = new(script);

            expression.Compile(byteCode);

            Instruction call = byteCode.code[^1];

            Assert.That(call.OpCode, Is.EqualTo(OpCode.Call));
            Assert.That(call.NumVal, Is.EqualTo(1));
        }

        [Test]
        public void ConstructorThrowsWhenArgumentsMissing()
        {
            Script script = new();
            ScriptLoadingContext context = new(script) { Lexer = new Lexer(0, string.Empty, true) };
            Expression callee = new FunctionExpressionStub(context, "broken::callee");

            SyntaxErrorException exception = Assert.Throws<SyntaxErrorException>(() =>
                new FunctionCallExpression(context, callee, null)
            )!;

            Assert.Multiple(() =>
            {
                Assert.That(exception.Message, Does.Contain("function arguments expected"));
                Assert.That(exception.IsPrematureStreamTermination, Is.True);
            });
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
                bc.Emit_Literal(DynValue.NewString("function-stub"));
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
