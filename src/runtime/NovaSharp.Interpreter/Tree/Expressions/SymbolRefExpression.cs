namespace NovaSharp.Interpreter.Tree.Expressions
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class SymbolRefExpression : Expression, IVariable
    {
        private readonly SymbolRef _ref;
        private readonly string _varName;

        public SymbolRefExpression(Token t, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _varName = t.Text;

            if (t.type == TokenType.VarArgs)
            {
                _ref = lcontext.Scope.Find(WellKnownSymbols.VARARGS);

                if (!lcontext.Scope.CurrentFunctionHasVarArgs())
                {
                    throw new SyntaxErrorException(t, "cannot use '...' outside a vararg function");
                }

                if (lcontext.IsDynamicExpression)
                {
                    throw new DynamicExpressionException(
                        "cannot use '...' in a dynamic expression."
                    );
                }
            }
            else
            {
                if (!lcontext.IsDynamicExpression)
                {
                    _ref = lcontext.Scope.Find(_varName);
                }
            }

            lcontext.Lexer.Next();
        }

        public SymbolRefExpression(ScriptLoadingContext lcontext, SymbolRef refr)
            : base(lcontext)
        {
            _ref = refr;

            if (lcontext.IsDynamicExpression)
            {
                throw new DynamicExpressionException(
                    "Unsupported symbol reference expression detected."
                );
            }
        }

        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.Emit_Load(_ref);
        }

        public void CompileAssignment(Execution.VM.ByteCode bc, int stackofs, int tupleidx)
        {
            bc.Emit_Store(_ref, stackofs, tupleidx);
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            return context.EvaluateSymbolByName(_varName);
        }

        public override SymbolRef FindDynamic(ScriptExecutionContext context)
        {
            return context.FindSymbolByName(_varName);
        }
    }
}
