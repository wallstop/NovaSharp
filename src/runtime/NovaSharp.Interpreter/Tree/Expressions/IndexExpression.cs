namespace NovaSharp.Interpreter.Tree.Expressions
{
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    internal class IndexExpression : Expression, IVariable
    {
        private readonly Expression _baseExp;
        private readonly Expression _indexExp;
        private readonly string _name;

        public IndexExpression(
            Expression baseExp,
            Expression indexExp,
            ScriptLoadingContext lcontext
        )
            : base(lcontext)
        {
            _baseExp = baseExp;
            _indexExp = indexExp;
        }

        public IndexExpression(Expression baseExp, string name, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _baseExp = baseExp;
            _name = name;
        }

        public override void Compile(ByteCode bc)
        {
            _baseExp.Compile(bc);

            if (_name != null)
            {
                bc.EmitIndex(DynValue.NewString(_name), true);
            }
            else if (_indexExp is LiteralExpression lit)
            {
                bc.EmitIndex(lit.Value);
            }
            else
            {
                _indexExp.Compile(bc);
                bc.EmitIndex(isExpList: (_indexExp is ExprListExpression));
            }
        }

        public void CompileAssignment(ByteCode bc, int stackofs, int tupleidx)
        {
            _baseExp.Compile(bc);

            if (_name != null)
            {
                bc.EmitIndexSet(stackofs, tupleidx, DynValue.NewString(_name), isNameIndex: true);
            }
            else if (_indexExp is LiteralExpression lit)
            {
                bc.EmitIndexSet(stackofs, tupleidx, lit.Value);
            }
            else
            {
                _indexExp.Compile(bc);
                bc.EmitIndexSet(stackofs, tupleidx, isExpList: (_indexExp is ExprListExpression));
            }
        }

        public override DynValue Eval(ScriptExecutionContext context)
        {
            DynValue b = _baseExp.Eval(context).ToScalar();
            DynValue i =
                _indexExp != null ? _indexExp.Eval(context).ToScalar() : DynValue.NewString(_name);

            if (b.Type != DataType.Table)
            {
                throw new DynamicExpressionException("Attempt to index non-table.");
            }
            else if (i.IsNilOrNan())
            {
                throw new DynamicExpressionException("Attempt to index with nil or nan key.");
            }

            return b.Table.Get(i) ?? DynValue.Nil;
        }
    }
}
