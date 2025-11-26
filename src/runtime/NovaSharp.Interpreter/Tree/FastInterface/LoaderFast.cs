namespace NovaSharp.Interpreter.Tree.FastInterface
{
    using NovaSharp.Interpreter.Debugging;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Execution.Scopes;
    using NovaSharp.Interpreter.Execution.VM;
    using NovaSharp.Interpreter.Tree.Expressions;
    using NovaSharp.Interpreter.Tree.Lexer;
    using NovaSharp.Interpreter.Tree.Statements;

    /// <summary>
    /// Provides optimized entry points for parsing Lua source into NovaSharp AST/bytecode.
    /// </summary>
    /// <remarks>
    /// These helpers mirror <see cref="Script.LoadString" /> but avoid allocations by using the
    /// streamlined <c>LoaderFast</c> pipeline originally designed for the Unity runtime.
    /// </remarks>
    internal static class LoaderFast
    {
        /// <summary>
        /// Parses a dynamic expression (e.g., <c>Script.DoString</c>) and returns the AST wrapper.
        /// </summary>
        /// <param name="script">Script requesting the expression.</param>
        /// <param name="source">Lua source to parse.</param>
        /// <returns>The compiled expression tree ready for evaluation.</returns>
        /// <exception cref="SyntaxErrorException">
        /// Propagated when the source contains invalid syntax; the exception is decorated with script
        /// details before being rethrown.
        /// </exception>
        internal static DynamicExprExpression LoadDynamicExpr(Script script, SourceCode source)
        {
            ScriptLoadingContext lcontext = CreateLoadingContext(script, source);

            try
            {
                lcontext.IsDynamicExpression = true;
                lcontext.Anonymous = true;

                Expression exp;
                using (
                    script.PerformanceStats.StartStopwatch(
                        Diagnostics.PerformanceCounter.AstCreation
                    )
                )
                {
                    exp = Expression.Expr(lcontext);
                }

                return new DynamicExprExpression(exp, lcontext);
            }
            catch (SyntaxErrorException ex)
            {
                ex.DecorateMessage(script);
                ex.Rethrow();
                throw;
            }
        }

        private static ScriptLoadingContext CreateLoadingContext(Script script, SourceCode source)
        {
            return new ScriptLoadingContext(script)
            {
                Scope = new BuildTimeScope(),
                Source = source,
                Lexer = new Lexer(source.SourceId, source.Code, true),
            };
        }

        /// <summary>
        /// Parses a full Lua chunk and emits bytecode instructions for it.
        /// </summary>
        /// <param name="script">Script used for diagnostics and stopwatch tracking.</param>
        /// <param name="source">Lua chunk being compiled.</param>
        /// <param name="bytecode">Bytecode builder that receives the chunk.</param>
        /// <returns>The instruction pointer pointing at the first opcode of the emitted chunk.</returns>
        /// <exception cref="SyntaxErrorException">
        /// Propagated when the source cannot be parsed; the exception is decorated with script
        /// context before it is rethrown.
        /// </exception>
        internal static int LoadChunk(Script script, SourceCode source, ByteCode bytecode)
        {
            ScriptLoadingContext lcontext = CreateLoadingContext(script, source);
            try
            {
                ChunkStatement stat;

                using (
                    script.PerformanceStats.StartStopwatch(
                        Diagnostics.PerformanceCounter.AstCreation
                    )
                )
                {
                    stat = new ChunkStatement(lcontext);
                }

                int beginIp = -1;

                //var srcref = new SourceRef(source.SourceID);

                using (
                    script.PerformanceStats.StartStopwatch(
                        Diagnostics.PerformanceCounter.Compilation
                    )
                )
                using (bytecode.EnterSource(null))
                {
                    bytecode.EmitNop($"Begin chunk {source.Name}");
                    beginIp = bytecode.GetJumpPointForLastInstruction();
                    stat.Compile(bytecode);
                    bytecode.EmitNop($"End chunk {source.Name}");
                }

                //Debug_DumpByteCode(bytecode, source.SourceID);

                return beginIp;
            }
            catch (SyntaxErrorException ex)
            {
                ex.DecorateMessage(script);
                ex.Rethrow();
                throw;
            }
        }

        /// <summary>
        /// Parses and compiles a Lua function whose source exists independently of the host chunk.
        /// </summary>
        /// <param name="script">Script requesting the compilation.</param>
        /// <param name="source">Lua source that contains the function body.</param>
        /// <param name="bytecode">Bytecode builder receiving the emitted function.</param>
        /// <param name="usesGlobalEnv">
        /// When true, the resulting closure captures the global environment instead of a local one.
        /// </param>
        /// <returns>The instruction pointer for the compiled function body.</returns>
        /// <exception cref="SyntaxErrorException">
        /// Propagated when the function source is invalid; the exception is decorated first.
        /// </exception>
        internal static int LoadFunction(
            Script script,
            SourceCode source,
            ByteCode bytecode,
            bool usesGlobalEnv
        )
        {
            ScriptLoadingContext lcontext = CreateLoadingContext(script, source);

            try
            {
                FunctionDefinitionExpression fnx;

                using (
                    script.PerformanceStats.StartStopwatch(
                        Diagnostics.PerformanceCounter.AstCreation
                    )
                )
                {
                    fnx = new FunctionDefinitionExpression(lcontext, usesGlobalEnv);
                }

                int beginIp = -1;

                //var srcref = new SourceRef(source.SourceID);

                using (
                    script.PerformanceStats.StartStopwatch(
                        Diagnostics.PerformanceCounter.Compilation
                    )
                )
                {
                    bytecode.EmitNop($"Begin function {source.Name}");
                    beginIp = fnx.CompileBody(bytecode, source.Name);
                    bytecode.EmitNop($"End function {source.Name}");
                }

                //Debug_DumpByteCode(bytecode, source.SourceID);

                return beginIp;
            }
            catch (SyntaxErrorException ex)
            {
                ex.DecorateMessage(script);
                ex.Rethrow();
                throw;
            }
        }
    }
}
