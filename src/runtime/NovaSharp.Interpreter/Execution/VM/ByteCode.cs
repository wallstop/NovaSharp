#define EmitDebug_OPS

namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    internal class ByteCode : RefIdObject
    {
        public List<Instruction> Code { get; } = new();
        public Script Script { get; private set; }
        private readonly List<SourceRef> _sourceRefStack = new();
        private SourceRef _currentSourceRef;

        internal LoopTracker LoopTracker = new();

        public ByteCode(Script script)
        {
            Script = script;
        }

        public IDisposable EnterSource(SourceRef sref)
        {
            return new SourceCodeStackGuard(sref, this);
        }

        private class SourceCodeStackGuard : IDisposable
        {
            private readonly ByteCode _bc;

            public SourceCodeStackGuard(SourceRef sref, ByteCode bc)
            {
                _bc = bc;
                _bc.PushSourceRef(sref);
            }

            public void Dispose()
            {
                _bc.PopSourceRef();
            }
        }

        public void PushSourceRef(SourceRef sref)
        {
            _sourceRefStack.Add(sref);
            _currentSourceRef = sref;
        }

        public void PopSourceRef()
        {
            _sourceRefStack.RemoveAt(_sourceRefStack.Count - 1);
            _currentSourceRef = (_sourceRefStack.Count > 0) ? _sourceRefStack[^1] : null;
        }

#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE) && (!(NETFX_CORE))
        public void Dump(string file)
        {
            StringBuilder sb = new();

            for (int i = 0; i < Code.Count; i++)
            {
                if (Code[i].OpCode == OpCode.Debug)
                {
                    sb.AppendFormat("    {0}\n", Code[i]);
                }
                else
                {
                    sb.AppendFormat("{0:X8}  {1}\n", i, Code[i]);
                }
            }

            File.WriteAllText(file, sb.ToString());
        }
#endif

        public int GetJumpPointForNextInstruction()
        {
            return Code.Count;
        }

        public int GetJumpPointForLastInstruction()
        {
            return Code.Count - 1;
        }

        public Instruction GetLastInstruction()
        {
            return Code[^1];
        }

        private Instruction AppendInstruction(Instruction c)
        {
            Code.Add(c);
            return c;
        }

        public Instruction EmitNop(string comment)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Nop, Name = comment }
            );
        }

        public Instruction EmitInvalid(string type)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Invalid, Name = type }
            );
        }

        public Instruction EmitPop(int num = 1)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Pop, NumVal = num }
            );
        }

        public void EmitCall(int argCount, string debugName)
        {
            AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Call,
                    NumVal = argCount,
                    Name = debugName,
                }
            );
        }

        public void EmitThisCall(int argCount, string debugName)
        {
            AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.ThisCall,
                    NumVal = argCount,
                    Name = debugName,
                }
            );
        }

        public Instruction EmitLiteral(DynValue value)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Literal, Value = value }
            );
        }

        public Instruction EmitJump(OpCode jumpOpCode, int idx, int optPar = 0)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = jumpOpCode,
                    NumVal = idx,
                    NumVal2 = optPar,
                }
            );
        }

        public Instruction EmitMkTuple(int cnt)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.MkTuple, NumVal = cnt }
            );
        }

        public Instruction EmitOperator(OpCode opcode)
        {
            Instruction i = AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = opcode }
            );

            if (opcode == OpCode.LessEq)
            {
                AppendInstruction(new Instruction(_currentSourceRef) { OpCode = OpCode.CNot });
            }

            if (opcode == OpCode.Eq || opcode == OpCode.Less)
            {
                AppendInstruction(new Instruction(_currentSourceRef) { OpCode = OpCode.ToBool });
            }

            return i;
        }

        [Conditional("EmitDebug_OPS")]
        public void EmitDebug(string str)
        {
            AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Debug,
                    Name = str.Substring(0, Math.Min(32, str.Length)),
                }
            );
        }

        public Instruction EmitEnter(RuntimeScopeBlock runtimeScopeBlock)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Enter,
                    NumVal = runtimeScopeBlock.From,
                    NumVal2 = runtimeScopeBlock.ToInclusive,
                    SymbolList = runtimeScopeBlock.ToBeClosed,
                }
            );
        }

        public Instruction EmitLeave(RuntimeScopeBlock runtimeScopeBlock)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Leave,
                    NumVal = runtimeScopeBlock.From,
                    NumVal2 = runtimeScopeBlock.To,
                    SymbolList = runtimeScopeBlock.ToBeClosed,
                }
            );
        }

        public Instruction EmitExit(RuntimeScopeBlock runtimeScopeBlock)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Exit,
                    NumVal = runtimeScopeBlock.From,
                    NumVal2 = runtimeScopeBlock.ToInclusive,
                    SymbolList = runtimeScopeBlock.ToBeClosed,
                }
            );
        }

        public Instruction EmitClean(RuntimeScopeBlock runtimeScopeBlock)
        {
            SymbolRef[] closers = Array.Empty<SymbolRef>();

            if (runtimeScopeBlock.ToBeClosed.Length > 0)
            {
                List<SymbolRef> subset = new();
                int threshold = runtimeScopeBlock.To;

                foreach (SymbolRef sym in runtimeScopeBlock.ToBeClosed)
                {
                    if (sym.IndexValue > threshold)
                    {
                        subset.Add(sym);
                    }
                }

                if (subset.Count > 0)
                {
                    closers = subset.ToArray();
                }
            }

            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Clean,
                    NumVal = runtimeScopeBlock.To + 1,
                    NumVal2 = runtimeScopeBlock.ToInclusive,
                    SymbolList = closers,
                }
            );
        }

        public Instruction EmitClosure(SymbolRef[] symbols, int jmpnum)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Closure,
                    SymbolList = symbols,
                    NumVal = jmpnum,
                }
            );
        }

        public Instruction EmitArgs(params SymbolRef[] symbols)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Args, SymbolList = symbols }
            );
        }

        public Instruction EmitRet(int retvals)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Ret, NumVal = retvals }
            );
        }

        public Instruction EmitToNum(int stage = 0)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.ToNum, NumVal = stage }
            );
        }

        public Instruction EmitIncr(int i)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Incr, NumVal = i }
            );
        }

        public Instruction EmitNewTable(bool shared)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.NewTable,
                    NumVal = shared ? 1 : 0,
                }
            );
        }

        public Instruction EmitIterPrep()
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.IterPrep }
            );
        }

        public Instruction EmitExpTuple(int stackOffset)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.ExpTuple,
                    NumVal = stackOffset,
                }
            );
        }

        public Instruction EmitIterUpd()
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.IterUpd }
            );
        }

        public Instruction EmitMeta(
            string funcName,
            OpCodeMetadataType metaType,
            DynValue value = null
        )
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Meta,
                    Name = funcName,
                    NumVal2 = (int)metaType,
                    Value = value,
                }
            );
        }

        public Instruction EmitBeginFn(RuntimeScopeFrame stackFrame)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.BeginFn,
                    SymbolList = stackFrame.DebugSymbols.ToArray(),
                    NumVal = stackFrame.Count,
                    NumVal2 = stackFrame.ToFirstBlock,
                }
            );
        }

        public Instruction EmitScalar()
        {
            return AppendInstruction(new Instruction(_currentSourceRef) { OpCode = OpCode.Scalar });
        }

        public int EmitLoad(SymbolRef sym)
        {
            switch (sym.Type)
            {
                case SymbolRefType.Global:
                    EmitLoad(sym.EnvironmentRef);
                    AppendInstruction(
                        new Instruction(_currentSourceRef)
                        {
                            OpCode = OpCode.Index,
                            Value = DynValue.NewString(sym.NameValue),
                        }
                    );
                    return 2;
                case SymbolRefType.Local:
                    AppendInstruction(
                        new Instruction(_currentSourceRef) { OpCode = OpCode.Local, Symbol = sym }
                    );
                    return 1;
                case SymbolRefType.Upvalue:
                    AppendInstruction(
                        new Instruction(_currentSourceRef) { OpCode = OpCode.Upvalue, Symbol = sym }
                    );
                    return 1;
                default:
                    throw new InternalErrorException("Unexpected symbol type : {0}", sym);
            }
        }

        public int EmitStore(SymbolRef sym, int stackofs, int tupleidx)
        {
            switch (sym.Type)
            {
                case SymbolRefType.Global:
                    EmitLoad(sym.EnvironmentRef);
                    AppendInstruction(
                        new Instruction(_currentSourceRef)
                        {
                            OpCode = OpCode.IndexSet,
                            Symbol = sym,
                            NumVal = stackofs,
                            NumVal2 = tupleidx,
                            Value = DynValue.NewString(sym.NameValue),
                        }
                    );
                    return 2;
                case SymbolRefType.Local:
                    AppendInstruction(
                        new Instruction(_currentSourceRef)
                        {
                            OpCode = OpCode.StoreLcl,
                            Symbol = sym,
                            NumVal = stackofs,
                            NumVal2 = tupleidx,
                        }
                    );
                    return 1;
                case SymbolRefType.Upvalue:
                    AppendInstruction(
                        new Instruction(_currentSourceRef)
                        {
                            OpCode = OpCode.StoreUpv,
                            Symbol = sym,
                            NumVal = stackofs,
                            NumVal2 = tupleidx,
                        }
                    );
                    return 1;
                default:
                    throw new InternalErrorException("Unexpected symbol type : {0}", sym);
            }
        }

        public Instruction EmitTblInitN()
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.TblInitN }
            );
        }

        public Instruction EmitTblInitI(bool lastpos)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.TblInitI,
                    NumVal = lastpos ? 1 : 0,
                }
            );
        }

        public Instruction EmitIndex(
            DynValue index = null,
            bool isNameIndex = false,
            bool isExpList = false
        )
        {
            OpCode o;
            if (isNameIndex)
            {
                o = OpCode.IndexN;
            }
            else if (isExpList)
            {
                o = OpCode.IndexL;
            }
            else
            {
                o = OpCode.Index;
            }

            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = o, Value = index }
            );
        }

        public Instruction EmitIndexSet(
            int stackofs,
            int tupleidx,
            DynValue index = null,
            bool isNameIndex = false,
            bool isExpList = false
        )
        {
            OpCode o;
            if (isNameIndex)
            {
                o = OpCode.IndexSetN;
            }
            else if (isExpList)
            {
                o = OpCode.IndexSetL;
            }
            else
            {
                o = OpCode.IndexSet;
            }

            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = o,
                    NumVal = stackofs,
                    NumVal2 = tupleidx,
                    Value = index,
                }
            );
        }

        public Instruction EmitCopy(int numval)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Copy, NumVal = numval }
            );
        }

        public Instruction EmitSwap(int p1, int p2)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef)
                {
                    OpCode = OpCode.Swap,
                    NumVal = p1,
                    NumVal2 = p2,
                }
            );
        }
    }
}
