namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Represents <c>table[key]</c> or <c>table.name</c> expressions and knows how to compile them.
    /// </summary>
    /// <remarks>
    /// The node also implements <see cref="IVariable" /> so the same AST can emit setters when the
    /// expression appears on the left-hand side of an assignment.
    /// </remarks>
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

        /// <summary>
        /// Emits bytecode that loads the base table and pushes the requested field value.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the emitted opcodes.</param>
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

        /// <summary>
        /// Emits bytecode for assigning into the indexed value found by this expression.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the emitted opcodes.</param>
        /// <param name="stackofs">Stack offset where the value being assigned resides.</param>
        /// <param name="tupleidx">Tuple index when the assignment consumes a multi-return value.</param>
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

        /// <summary>
        /// Evaluates the index at runtime and returns the referenced value from the table.
        /// </summary>
        /// <param name="context">Execution context providing the table and index values.</param>
        /// <returns>The resolved value or <see cref="DynValue.Nil" /> when the entry is missing.</returns>
        /// <exception cref="DynamicExpressionException">
        /// Thrown when the base expression does not evaluate to a table or the key is invalid.
        /// </exception>
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
