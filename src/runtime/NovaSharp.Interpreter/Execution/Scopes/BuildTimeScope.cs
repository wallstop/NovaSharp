namespace NovaSharp.Interpreter.Execution.Scopes
{
    using System.Collections.Generic;
    using System.Linq;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tree.Statements;

    internal class BuildTimeScope
    {
        private readonly List<BuildTimeScopeFrame> _frames = new();
        private readonly List<IClosureBuilder> _closureBuilders = new();

        public void PushFunction(IClosureBuilder closureBuilder, bool hasVarArgs)
        {
            _closureBuilders.Add(closureBuilder);
            _frames.Add(new BuildTimeScopeFrame(hasVarArgs));
        }

        public void PushBlock()
        {
            _frames.Last().PushBlock();
        }

        public RuntimeScopeBlock PopBlock()
        {
            return _frames.Last().PopBlock();
        }

        public RuntimeScopeFrame PopFunction()
        {
            BuildTimeScopeFrame last = _frames.Last();
            last.ResolveLRefs();
            _frames.RemoveAt(_frames.Count - 1);

            _closureBuilders.RemoveAt(_closureBuilders.Count - 1);

            return last.GetRuntimeFrameData();
        }

        public SymbolRef Find(string name)
        {
            SymbolRef local = _frames.Last().Find(name);

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

        public SymbolRef CreateGlobalReference(string name)
        {
            if (name == WellKnownSymbols.ENV)
            {
                throw new InternalErrorException("_ENV passed in CreateGlobalReference");
            }

            SymbolRef env = Find(WellKnownSymbols.ENV);
            return SymbolRef.Global(name, env);
        }

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
                return _closureBuilders[currentFrame + 1].CreateUpvalue(this, symb);
            }

            SymbolRef upvalue = CreateUpValue(
                buildTimeScope,
                symb,
                closuredFrame,
                currentFrame - 1
            );

            return _closureBuilders[currentFrame + 1].CreateUpvalue(this, upvalue);
        }

        public SymbolRef DefineLocal(string name)
        {
            return _frames.Last().DefineLocal(name);
        }

        public SymbolRef TryDefineLocal(string name)
        {
            return _frames.Last().TryDefineLocal(name);
        }

        public bool CurrentFunctionHasVarArgs()
        {
            return _frames.Last().HasVarArgs;
        }

        internal void DefineLabel(LabelStatement label)
        {
            _frames.Last().DefineLabel(label);
        }

        internal void RegisterGoto(GotoStatement gotostat)
        {
            _frames.Last().RegisterGoto(gotostat);
        }
    }
}
