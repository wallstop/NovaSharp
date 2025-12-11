namespace WallstopStudios.NovaSharp.Interpreter.Execution.Scopes
{
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Tree.Statements;

    /// <summary>
    /// Builds the lexical environment for a function while the parser walks the abstract syntax tree.
    /// </summary>
    /// <remarks>
    /// The scope graph mirrors the rules described in Lua 5.4 §3.5 so that locals, nested functions, and
    /// the implicit <c>_ENV</c> reference can be resolved without relying on reflection or runtime lookups.
    /// </remarks>
    internal class BuildTimeScope
    {
        private readonly List<BuildTimeScopeFrame> _frames = new();
        private readonly List<IClosureBuilder> _closureBuilders = new();

        /// <summary>
        /// Gets the innermost (current) scope frame without LINQ overhead.
        /// </summary>
        private BuildTimeScopeFrame CurrentFrame => _frames[_frames.Count - 1];

        /// <summary>
        /// Pushes a new function frame so local declarations and upvalues can be emitted for the nested body.
        /// </summary>
        /// <param name="closureBuilder">Callback that materializes captured variables for the compiled function.</param>
        /// <param name="hasVarArgs">Whether the function declares <c>...</c> and therefore needs the extra slot.</param>
        public void PushFunction(IClosureBuilder closureBuilder, bool hasVarArgs)
        {
            _closureBuilders.Add(closureBuilder);
            _frames.Add(new BuildTimeScopeFrame(hasVarArgs));
        }

        /// <summary>
        /// Starts a new lexical block within the innermost frame (e.g., a <c>do ... end</c> or loop body).
        /// </summary>
        public void PushBlock()
        {
            CurrentFrame.PushBlock();
        }

        /// <summary>
        /// Completes the current block and returns the runtime metadata that constrains the locals defined in it.
        /// </summary>
        /// <returns>The <see cref="RuntimeScopeBlock"/> generated for the block.</returns>
        public RuntimeScopeBlock PopBlock()
        {
            return CurrentFrame.PopBlock();
        }

        /// <summary>
        /// Finalizes the innermost function frame, resolving pending locals/upvalues and returning the runtime scope data.
        /// </summary>
        /// <returns>The <see cref="RuntimeScopeFrame"/> describing locals for the compiled function.</returns>
        public RuntimeScopeFrame PopFunction()
        {
            BuildTimeScopeFrame last = CurrentFrame;
            last.ResolveLRefs();
            _frames.RemoveAt(_frames.Count - 1);

            _closureBuilders.RemoveAt(_closureBuilders.Count - 1);

            return last.GetRuntimeFrameData();
        }

        /// <summary>
        /// Resolves a symbol by name, searching the active block stack, enclosing functions, and finally the global environment.
        /// </summary>
        /// <param name="name">Symbol name requested by the parser.</param>
        /// <returns>A local, upvalue, or global symbol reference.</returns>
        public SymbolRef Find(string name)
        {
            SymbolRef local = CurrentFrame.Find(name);

            if (local != null)
            {
                return local;
            }

            for (int i = _frames.Count - 2; i >= 0; i--)
            {
                SymbolRef symb = _frames[i].Find(name);

                if (symb != null)
                {
                    symb = CreateUpValue(this, symb, i, _frames.Count - 2);

                    if (symb != null)
                    {
                        return symb;
                    }
                }
            }

            return CreateGlobalReference(name);
        }

        /// <summary>
        /// Creates a global symbol reference using the innermost <c>_ENV</c> binding.
        /// </summary>
        /// <param name="name">Name of the global the compiler needs to access.</param>
        /// <exception cref="InternalErrorException">Thrown when <c>_ENV</c> resolution failed earlier in the pipeline.</exception>
        public SymbolRef CreateGlobalReference(string name)
        {
            if (name == WellKnownSymbols.ENV)
            {
                throw new InternalErrorException("_ENV passed in CreateGlobalReference");
            }

            SymbolRef env = Find(WellKnownSymbols.ENV);
            return SymbolRef.Global(name, env);
        }

        /// <summary>
        /// Forces the current function to capture <c>_ENV</c> as an upvalue so globals remain addressable even when not explicitly referenced.
        /// </summary>
        public void ForceEnvUpValue()
        {
            Find(WellKnownSymbols.ENV);
        }

        private SymbolRef CreateUpValue(
            BuildTimeScope buildTimeScope,
            SymbolRef symb,
            int closuredFrame,
            int currentFrame
        )
        {
            // it's a 0-level upvalue. Just create it and we're done.
            if (closuredFrame == currentFrame)
            {
                return _closureBuilders[currentFrame + 1].CreateUpValue(this, symb);
            }

            SymbolRef upvalue = CreateUpValue(
                buildTimeScope,
                symb,
                closuredFrame,
                currentFrame - 1
            );

            return _closureBuilders[currentFrame + 1].CreateUpValue(this, upvalue);
        }

        /// <summary>
        /// Declares a new local symbol in the current block.
        /// </summary>
        /// <param name="name">Name of the local.</param>
        /// <param name="flags">Attributes applied to the symbol (e.g., <c>ToBeClosed</c>).</param>
        /// <returns>The symbol that will be assigned a slot at emit time.</returns>
        public SymbolRef DefineLocal(string name, SymbolRefAttributes flags = default)
        {
            return CurrentFrame.DefineLocal(name, flags);
        }

        /// <summary>
        /// Declares a local if one with the same name is not already live in the current block; otherwise, renames the previous symbol per Lua's goto rules.
        /// </summary>
        /// <param name="name">Name requested by the parser.</param>
        /// <param name="flags">Attributes applied to the symbol.</param>
        /// <returns>The newly declared symbol.</returns>
        public SymbolRef TryDefineLocal(string name, SymbolRefAttributes flags = default)
        {
            return CurrentFrame.TryDefineLocal(name, flags);
        }

        /// <summary>
        /// Indicates whether the current function frame reserves a vararg tuple (<c>...</c>).
        /// </summary>
        public bool CurrentFunctionHasVarArgs()
        {
            return CurrentFrame.HasVarArgs;
        }

        /// <summary>
        /// Registers a label inside the active block so <c>goto</c> statements can resolve against it.
        /// </summary>
        /// <param name="label">The label being declared.</param>
        internal void DefineLabel(LabelStatement label)
        {
            CurrentFrame.DefineLabel(label);
        }

        /// <summary>
        /// Records a pending <c>goto</c> so it can be resolved once all labels in the block hierarchy are known.
        /// </summary>
        /// <param name="gotostat">The jump statement being processed.</param>
        internal void RegisterGoto(GotoStatement gotostat)
        {
            CurrentFrame.RegisterGoto(gotostat);
        }
    }
}
