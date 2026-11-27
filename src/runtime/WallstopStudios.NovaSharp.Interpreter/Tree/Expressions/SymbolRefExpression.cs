namespace WallstopStudios.NovaSharp.Interpreter.Tree.Expressions
{
    using global::NovaSharp;
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
        private readonly Token _nameToken;
        private readonly bool _allowsConstAssignment;

        public SymbolRefExpression(Token t, ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            _varName = t.text;
            _nameToken = t;

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
            // Declarations and the compiler's loop-variable writes initialize a const slot;
            // only source-level assignments resolve symbols through the token constructor.
            _allowsConstAssignment = true;

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
        /// Gets the local or upvalue this expression writes directly, if it is a direct target.
        /// </summary>
        internal bool TryGetDirectAssignmentSymbol(out SymbolRef symbol)
        {
            if (
                _ref != null
                && (_ref.Type == SymbolRefType.Local || _ref.Type == SymbolRefType.UpValue)
            )
            {
                symbol = _ref;
                return true;
            }

            symbol = null;
            return false;
        }

        /// <summary>
        /// Prepares a global target by deciding whether its environment is read before the
        /// right-hand side or when the store executes.
        /// </summary>
        internal AssignmentTarget PrepareAssignmentTarget(Execution.VM.ByteCode bc)
        {
            if (_ref == null || _ref.Type != SymbolRefType.Global)
            {
                return AssignmentTarget.Direct(this);
            }

            AssignmentOperand baseOperand;
            if (
                AssignmentTarget.CanDeferSymbolRead(
                    _ref.EnvironmentRef,
                    bc,
                    isIndexOperand: false,
                    hasConstantIndex: true
                )
            )
            {
                baseOperand = AssignmentOperand.FromSymbol(_ref.EnvironmentRef);
            }
            else
            {
                bc.EmitLoad(_ref.EnvironmentRef);
                baseOperand = AssignmentOperand.Stack();
            }

            return AssignmentTarget.Indexed(
                baseOperand,
                AssignmentOperand.FromLiteral(LuaValue.NewString(_ref.NameValue)),
                isNameIndex: true,
                isExpList: false,
                baseName: null
            );
        }

        /// <summary>
        /// Emits a store instruction targeting the resolved symbol.
        /// </summary>
        /// <param name="bc">Bytecode builder receiving the store opcode.</param>
        /// <param name="stackofs">Stack offset of the value being assigned.</param>
        /// <param name="tupleidx">Tuple index when assignment consumes multi-return values.</param>
        public void CompileAssignment(Execution.VM.ByteCode bc, int stackofs, int tupleidx)
        {
            if (!_allowsConstAssignment && _ref.IsImmutable)
            {
                throw new SyntaxErrorException(
                    _nameToken,
                    "{0}: attempt to assign to const variable '{1}'",
                    _nameToken.GetSourceRef().FormatLocation(bc.Script),
                    _varName
                )
                {
                    DoNotDecorateMessage = true,
                };
            }

            bc.EmitStore(_ref, stackofs, tupleidx);
        }

        /// <summary>
        /// Looks up the referenced symbol at runtime using the provided context.
        /// </summary>
        /// <param name="context">Execution context that owns the symbol tables.</param>
        /// <returns>The resolved value for the identifier.</returns>
        public override LuaValue Eval(ScriptExecutionContext context)
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
