namespace NovaSharp.Interpreter.Tree.Statements
{
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Lexer;

    /// <summary>
    /// Root statement for a compiled chunk; wraps the top-level block in an implicit function so locals/env behave per Lua rules.
    /// </summary>
    internal class ChunkStatement : Statement, IClosureBuilder
    {
        private readonly CompositeStatement _block;
        private readonly RuntimeScopeFrame _stackFrame;
        private readonly SymbolRef _env;
        private readonly SymbolRef _varArgs;

        /// <summary>
        /// Parses the entire chunk, ensuring the file terminates at EOF and setting up the implicit `_ENV` and `...` locals.
        /// </summary>
        public ChunkStatement(ScriptLoadingContext lcontext)
            : base(lcontext)
        {
            lcontext.Scope.PushFunction(this, true);
            _env = lcontext.Scope.DefineLocal(WellKnownSymbols.ENV);
            _varArgs = lcontext.Scope.DefineLocal(WellKnownSymbols.VARARGS);

            _block = new CompositeStatement(lcontext);

            if (lcontext.Lexer.Current.Type != TokenType.Eof)
            {
                throw new SyntaxErrorException(
                    lcontext.Lexer.Current,
                    "<eof> expected near '{0}'",
                    lcontext.Lexer.Current.Text
                );
            }

            _stackFrame = lcontext.Scope.PopFunction();
        }

        /// <summary>
        /// Emits the implicit chunk function and compiles the contained block.
        /// </summary>
        public override void Compile(ByteCode bc)
        {
            Instruction meta = bc.EmitMeta("<chunk-root>", OpCodeMetadataType.ChunkEntrypoint);
            int metaip = bc.GetJumpPointForLastInstruction();

            bc.EmitBeginFn(_stackFrame);
            bc.EmitArgs(_varArgs);

            bc.EmitLoad(SymbolRef.Upvalue(WellKnownSymbols.ENV, 0));
            bc.EmitStore(_env, 0, 0);
            bc.EmitPop();

            _block.Compile(bc);
            bc.EmitRet(0);

            meta.NumVal = bc.GetJumpPointForLastInstruction() - metaip;
        }

        /// <summary>
        /// Chunk statements do not create additional upvalues; returns <c>null</c> so callers fall back to default resolution.
        /// </summary>
        public SymbolRef CreateUpvalue(BuildTimeScope scope, SymbolRef symbol)
        {
            return null;
        }
    }
}
