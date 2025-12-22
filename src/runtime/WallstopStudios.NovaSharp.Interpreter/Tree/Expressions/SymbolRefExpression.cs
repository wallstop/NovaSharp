namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Resolves identifiers (locals, globals, or <c>...</c>) at compile time and runtime.
    /// </summary>
    /// <remarks>
    /// The node implements <see cref="IVariable" /> so compiled assignments reuse the same symbol
    /// resolution metadata captured from <see cref="ScriptLoadingContext" />.
    /// </remarks>
    internal class SymbolRefExpression : Expression, IVariable
    {
        private readonly SymbolRef _ref;
        private readonly string _varName;

        public SymbolRefExpression(Token t, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _varName = t.text;

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

        /// <summary>
        /// Emits a load instruction for the resolved symbol.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the load opcode.</param>
        public override void Compile(Execution.VM.ByteCode bc)
        {
            bc.EmitLoad(_ref);
        }

        /// <summary>
        /// Emits a store instruction targeting the resolved symbol.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the store opcode.</param>
        /// <param name="stackofs">Stack offset of the value being assigned.</param>
        /// <param name="tupleidx">Tuple index when assignment consumes multi-return values.</param>
        public void CompileAssignment(Execution.VM.ByteCode bc, int stackofs, int tupleidx)
        {
            bc.EmitStore(_ref, stackofs, tupleidx);
        }

        /// <summary>
        /// Looks up the referenced symbol at runtime using the provided context.
        /// </summary>
        /// <param name="context">Execution context that owns the symbol tables.</param>
        /// <returns>The resolved value for the identifier.</returns>
        public override DynValue Eval(ScriptExecutionContext context)
        {
            return context.EvaluateSymbolByName(_varName);
        }

        /// <summary>
        /// Gets the underlying symbol reference for this expression.
        /// </summary>
        /// <returns>The <see cref="SymbolRef" /> associated with this expression, or null if not available.</returns>
        internal SymbolRef GetSymbolRef()
        {
            return _ref;
        }

        /// <summary>
        /// Resolves the symbol reference when compiling a dynamic expression.
        /// </summary>
        /// <param name="context">Execution context used to look up the symbol.</param>
        /// <returns>The <see cref="SymbolRef" /> for the identifier.</returns>
        public override SymbolRef FindDynamic(ScriptExecutionContext context)
        {
            return context.FindSymbolByName(_varName);
        }
    }
}
