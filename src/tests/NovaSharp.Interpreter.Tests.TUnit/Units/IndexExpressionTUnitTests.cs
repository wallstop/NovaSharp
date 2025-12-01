#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
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

    public sealed class IndexExpressionTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CompileEmitsNameIndexInstruction()
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

            await Assert.That(bc.Code[^1].OpCode).IsEqualTo(OpCode.IndexN);
        }

        [global::TUnit.Core.Test]
        public async Task CompileEmitsLiteralIndexInstruction()
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

            await Assert.That(bc.Code[^1].OpCode).IsEqualTo(OpCode.Index);
            await Assert.That(bc.Code[^1].Value.String).IsEqualTo("literal");
        }

        [global::TUnit.Core.Test]
        public async Task CompileEmitsListIndexInstruction()
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

            await Assert.That(bc.Code[^1].OpCode).IsEqualTo(OpCode.IndexL);
        }

        [global::TUnit.Core.Test]
        public async Task CompileAssignmentEmitsNameIndexInstruction()
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

            await Assert.That(bc.Code[^1].OpCode).IsEqualTo(OpCode.IndexSetN);
        }

        [global::TUnit.Core.Test]
        public async Task CompileAssignmentEmitsLiteralIndexInstruction()
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

            await Assert.That(bc.Code[^1].OpCode).IsEqualTo(OpCode.IndexSet);
            await Assert.That(bc.Code[^1].Value.String).IsEqualTo("literal");
        }

        [global::TUnit.Core.Test]
        public async Task CompileAssignmentEmitsListIndexInstruction()
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

            await Assert.That(bc.Code[^1].OpCode).IsEqualTo(OpCode.IndexSetL);
        }

        [global::TUnit.Core.Test]
        public async Task EvalThrowsWhenBaseIsNotTable()
        {
            Script script = new();
            ScriptExecutionContext execContext = script.CreateDynamicExecutionContext();
            ScriptLoadingContext context = CreateContext(script);

            IndexExpression expression = new(
                StubExpression.WithValue(context, DynValue.NewNumber(1)),
                StubExpression.WithValue(context, DynValue.NewString("field")),
                context
            );

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expression.Eval(execContext)
            )!;
            await Assert.That(exception.Message).Contains("Attempt to index non-table");
        }

        [global::TUnit.Core.Test]
        public async Task EvalThrowsWhenKeyIsNil()
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

            DynamicExpressionException exception = Assert.Throws<DynamicExpressionException>(() =>
                expression.Eval(execContext)
            )!;
            await Assert.That(exception.Message).Contains("nil or nan");
        }

        [global::TUnit.Core.Test]
        public async Task EvalReturnsNilWhenKeyMissing()
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
            await Assert.That(result.IsNil()).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task EvalReturnsExistingValue()
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
            await Assert.That(result.Number).IsEqualTo(42d);
        }

        [global::TUnit.Core.Test]
        public async Task EvalUsesNameWhenIndexExpressionIsNull()
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
            await Assert.That(result.String).IsEqualTo("hit");
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
            public SymbolRef CreateUpValue(BuildTimeScope scope, SymbolRef symbol)
            {
                return symbol;
            }
        }
    }
}
#pragma warning restore CA2007
