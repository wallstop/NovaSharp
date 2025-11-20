namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NUnit.Framework;

    [TestFixture]
    public sealed class IndexExpressionTests
    {
        [Test]
        public void CompileEmitsNameIndexInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = CreateContext(script);
            StubExpression tableExpr = StubExpression.WithValue(
                context,
                DynValue.NewTable(new Table(script))
            );
            IndexExpression expression = new(tableExpr, "field", context);

            ByteCode bc = new(script);
            expression.Compile(bc);

            Assert.That(bc.Code[^1].OpCode, Is.EqualTo(OpCode.IndexN));
        }

        [Test]
        public void CompileEmitsLiteralIndexInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = CreateContext(script);
            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(new Table(script))),
                CreateLiteralExpression(context, "literal"),
                context
            );

            ByteCode bc = new(script);
            expression.Compile(bc);

            Assert.Multiple(() =>
            {
                Assert.That(bc.Code[^1].OpCode, Is.EqualTo(OpCode.Index));
                Assert.That(bc.Code[^1].Value.String, Is.EqualTo("literal"));
            });
        }

        [Test]
        public void CompileEmitsListIndexInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = CreateContext(script);
            List<Expression> list = new()
            {
                StubExpression.WithValue(context, DynValue.NewNumber(1)),
                StubExpression.WithValue(context, DynValue.NewNumber(2)),
            };
            ExprListExpression listExpression = new(list, context);
            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(new Table(script))),
                listExpression,
                context
            );

            ByteCode bc = new(script);
            expression.Compile(bc);

            Assert.That(bc.Code[^1].OpCode, Is.EqualTo(OpCode.IndexL));
        }

        [Test]
        public void CompileAssignmentEmitsNameIndexInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = CreateContext(script);
            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(new Table(script))),
                "field",
                context
            );

            ByteCode bc = new(script);
            expression.CompileAssignment(bc, stackofs: 0, tupleidx: 0);

            Assert.That(bc.Code[^1].OpCode, Is.EqualTo(OpCode.IndexSetN));
        }

        [Test]
        public void CompileAssignmentEmitsLiteralIndexInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = CreateContext(script);
            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(new Table(script))),
                CreateLiteralExpression(context, "literal"),
                context
            );

            ByteCode bc = new(script);
            expression.CompileAssignment(bc, 0, 0);

            Assert.Multiple(() =>
            {
                Assert.That(bc.Code[^1].OpCode, Is.EqualTo(OpCode.IndexSet));
                Assert.That(bc.Code[^1].Value.String, Is.EqualTo("literal"));
            });
        }

        [Test]
        public void CompileAssignmentEmitsListIndexInstruction()
        {
            Script script = new();
            ScriptLoadingContext context = CreateContext(script);
            List<Expression> list = new()
            {
                StubExpression.WithValue(context, DynValue.NewString("a")),
                StubExpression.WithValue(context, DynValue.NewString("b")),
            };
            ExprListExpression listExpression = new(list, context);
            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(new Table(script))),
                listExpression,
                context
            );

            ByteCode bc = new(script);
            expression.CompileAssignment(bc, 0, 0);

            Assert.That(bc.Code[^1].OpCode, Is.EqualTo(OpCode.IndexSetL));
        }

        [Test]
        public void EvalThrowsWhenBaseIsNotTable()
        {
            Script script = new();
            ScriptExecutionContext execContext = script.CreateDynamicExecutionContext();
            ScriptLoadingContext context = CreateContext(script);

            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewNumber(1)),
                StubExpression.WithValue(context, DynValue.NewString("field")),
                context
            );

            Assert.That(
                () => expression.Eval(execContext),
                Throws
                    .TypeOf<DynamicExpressionException>()
                    .With.Message.Contains("Attempt to index non-table")
            );
        }

        [Test]
        public void EvalThrowsWhenKeyIsNil()
        {
            Script script = new();
            ScriptExecutionContext execContext = script.CreateDynamicExecutionContext();
            ScriptLoadingContext context = CreateContext(script);
            Table table = new(script);

            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(table)),
                StubExpression.WithValue(context, DynValue.Nil),
                context
            );

            Assert.That(
                () => expression.Eval(execContext),
                Throws.TypeOf<DynamicExpressionException>().With.Message.Contains("nil or nan")
            );
        }

        [Test]
        public void EvalReturnsNilWhenKeyMissing()
        {
            Script script = new();
            ScriptExecutionContext execContext = script.CreateDynamicExecutionContext();
            ScriptLoadingContext context = CreateContext(script);
            Table table = new(script);

            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(table)),
                StubExpression.WithValue(context, DynValue.NewString("missing")),
                context
            );

            DynValue result = expression.Eval(execContext);

            Assert.That(result.IsNil(), Is.True);
        }

        [Test]
        public void EvalReturnsExistingValue()
        {
            Script script = new();
            ScriptExecutionContext execContext = script.CreateDynamicExecutionContext();
            ScriptLoadingContext context = CreateContext(script);
            Table table = new(script);
            table.Set("field", DynValue.NewNumber(42));

            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(table)),
                StubExpression.WithValue(context, DynValue.NewString("field")),
                context
            );

            DynValue result = expression.Eval(execContext);
            Assert.That(result.Number, Is.EqualTo(42));
        }

        [Test]
        public void EvalUsesNameWhenIndexExpressionIsNull()
        {
            Script script = new();
            ScriptExecutionContext execContext = script.CreateDynamicExecutionContext();
            ScriptLoadingContext context = CreateContext(script);
            Table table = new(script);
            table.Set("direct", DynValue.NewString("hit"));

            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewTable(table)),
                "direct",
                context
            );

            DynValue result = expression.Eval(execContext);
            Assert.That(result.String, Is.EqualTo("hit"));
        }

        private static LiteralExpression CreateLiteralExpression(
            ScriptLoadingContext context,
            string text
        )
        {
            Token token = new(TokenType.String, 0, 0, 0, 0, 0, 0, 0) { Text = text };
            return new LiteralExpression(context, token);
        }

        private static ScriptLoadingContext CreateContext(Script script)
        {
            SourceCode source = new("units/index", string.Empty, script.SourceCodeCount, script);
            ScriptLoadingContext context = new(script)
            {
                Source = source,
                Scope = new BuildTimeScope(),
                Lexer = new Lexer(source.SourceId, string.Empty, true),
            };

            context.Scope.PushFunction(new PassthroughClosureBuilder(), hasVarArgs: false);
            context.Scope.PushBlock();
            return context;
        }

        private sealed class StubExpression : Expression
        {
            private readonly DynValue _value;

            private StubExpression(ScriptLoadingContext context, DynValue value)
                : base(context)
            {
                _value = value;
            }

            public static StubExpression WithValue(ScriptLoadingContext context, DynValue value) =>
                new(context, value);

            public override void Compile(ByteCode bc)
            {
                bc.EmitNop("stub");
            }

            public override DynValue Eval(ScriptExecutionContext context)
            {
                return _value;
            }
        }

        private sealed class PassthroughClosureBuilder : IClosureBuilder
        {
            public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol)
            {
                return symbol;
            }
        }
    }
}
