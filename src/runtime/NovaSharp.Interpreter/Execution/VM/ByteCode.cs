#define EmitDebug_OPS

namespace NovaSharp.Interpreter.Execution.VM
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Debugging;
    using Execution.Scopes;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;

    /// <summary>
    /// Provides the fluent API the parser uses to emit NovaSharp VM instructions for a compiled script.
    /// </summary>
    /// <remarks>
    /// Each method appends an <see cref="Instruction"/> configured for the Lua construct being compiled.
    /// The resulting buffer is consumed by <see cref="Processor"/> during execution.
    /// </remarks>
    internal class ByteCode : RefIdObject
    {
        /// <summary>
        /// Gets the ordered list of instructions produced for the chunk.
        /// </summary>
        public List<Instruction> Code { get; } = new();

        /// <summary>
        /// Gets the script that owns this bytecode buffer.
        /// </summary>
        public Script Script { get; private set; }
        private readonly List<SourceRef> _sourceRefStack = new();
        private SourceRef _currentSourceRef;

        /// <summary>
        /// Tracks active loop constructs while emitting so <c>break</c> statements can resolve jump sites.
        /// </summary>
        internal LoopTracker LoopTracker { get; } = new();

        /// <summary>
        /// Initializes the bytecode builder for the given script.
        /// </summary>
        /// <param name="script">Script that hosts the emitted code.</param>
        public ByteCode(Script script)
        {
            Script = script;
        }

        /// <summary>
        /// Enters the specified source reference, returning a guard that automatically pops the reference.
        /// </summary>
        /// <param name="sref">Source being compiled.</param>
        /// <returns>A disposable guard.</returns>
        public IDisposable EnterSource(SourceRef sref)
        {
            return new SourceCodeStackGuard(sref, this);
        }

        private class SourceCodeStackGuard : IDisposable
        {
            private readonly ByteCode _bc;

            /// <summary>
            /// Initializes the guard and pushes the supplied source reference on the stack.
            /// </summary>
            public SourceCodeStackGuard(SourceRef sref, ByteCode bc)
            {
                _bc = bc;
                _bc.PushSourceRef(sref);
            }

            /// <summary>
            /// Pops the source reference recorded by the guard.
            /// </summary>
            public void Dispose()
            {
                _bc.PopSourceRef();
            }
        }

        /// <summary>
        /// Pushes a source reference so the next instructions inherit its file/line info.
        /// </summary>
        public void PushSourceRef(SourceRef sref)
        {
            _sourceRefStack.Add(sref);
            _currentSourceRef = sref;
        }

        /// <summary>
        /// Pops the most recent source reference, restoring the previous one (if any).
        /// </summary>
        public void PopSourceRef()
        {
            _sourceRefStack.RemoveAt(_sourceRefStack.Count - 1);
            _currentSourceRef = (_sourceRefStack.Count > 0) ? _sourceRefStack[^1] : null;
        }

#if (!PCL) && ((!UNITY_5) || UNITY_STANDALONE) && (!(NETFX_CORE))
        /// <summary>
        /// Dumps the current instruction stream to disk for debugging purposes.
        /// </summary>
        /// <param name="file">Destination file path.</param>
        public void Dump(string file)
        {
            StringBuilder sb = new();

            for (int i = 0; i < Code.Count; i++)
            {
                if (Code[i].OpCode == OpCode.Debug)
                {
                    sb.Append("    ").Append(Code[i]).Append('\n');
                }
                else
                {
                    sb.Append(i.ToString("X8", CultureInfo.InvariantCulture))
                        .Append("  ")
                        .Append(Code[i])
                        .Append('\n');
                }
            }

            File.WriteAllText(file, sb.ToString());
        }
#endif

        /// <summary>
        /// Gets the instruction index where the next emission will land. Useful for jump patching.
        /// </summary>
        public int GetJumpPointForNextInstruction()
        {
            return Code.Count;
        }

        /// <summary>
        /// Gets the index of the last emitted instruction.
        /// </summary>
        public int GetJumpPointForLastInstruction()
        {
            return Code.Count - 1;
        }

        /// <summary>
        /// Gets the most recently appended instruction.
        /// </summary>
        public Instruction GetLastInstruction()
        {
            return Code[^1];
        }

        private Instruction AppendInstruction(Instruction c)
        {
            Code.Add(c);
            return c;
        }

        /// <summary>
        /// Emits a no-op instruction, typically used for breakpoints or placeholders.
        /// </summary>
        /// <param name="comment">Friendly name rendered in disassembly.</param>
        public Instruction EmitNop(string comment)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Nop, Name = comment }
            );
        }

        /// <summary>
        /// Emits the invalid opcode for guard paths that should never execute.
        /// </summary>
        /// <param name="type">Reason the code path is invalid.</param>
        public Instruction EmitInvalid(string type)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Invalid, Name = type }
            );
        }

        /// <summary>
        /// Emits a pop instruction that removes the specified number of values from the stack.
        /// </summary>
        /// <param name="num">Values to pop.</param>
        public Instruction EmitPop(int num = 1)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Pop, NumVal = num }
            );
        }

        /// <summary>
        /// Emits a function call with the specified argument count.
        /// </summary>
        /// <param name="argCount">Argument count passed to the callee.</param>
        /// <param name="debugName">Name describing the call site for debugging.</param>
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

        /// <summary>
        /// Emits a method call that preserves the implicit <c>self</c> receiver.
        /// </summary>
        /// <param name="argCount">Argument count, including the receiver.</param>
        /// <param name="debugName">Name describing the call site.</param>
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

        /// <summary>
        /// Emits a literal value (number/string/table/function/etc.).
        /// </summary>
        public Instruction EmitLiteral(DynValue value)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Literal, Value = value }
            );
        }

        /// <summary>
        /// Emits a jump instruction with the provided opcode and target.
        /// </summary>
        /// <param name="jumpOpCode">The conditional/unconditional jump opcode.</param>
        /// <param name="idx">Jump destination index.</param>
        /// <param name="optPar">Optional parameter (e.g., tuple size).</param>
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

        /// <summary>
        /// Emits a tuple construction instruction that aggregates multiple stack values.
        /// </summary>
        /// <param name="cnt">Number of values in the tuple.</param>
        public Instruction EmitMkTuple(int cnt)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.MkTuple, NumVal = cnt }
            );
        }

        /// <summary>
        /// Emits a unary/binary operator opcode and applies the Lua boolean semantics when needed.
        /// </summary>
        /// <param name="opcode">Operator opcode.</param>
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
        /// <summary>
        /// Emits a debug marker (only when EmitDebug_OPS is defined) that surfaces in disassembly.
        /// </summary>
        /// <param name="str">Debug description.</param>
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

        /// <summary>
        /// Emits an <c>Enter</c> instruction to extend the scope to the specified runtime block.
        /// </summary>
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

        /// <summary>
        /// Emits a <c>Leave</c> instruction to shrink the scope when the block ends.
        /// </summary>
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

        /// <summary>
        /// Emits an <c>Exit</c> instruction to abort execution and clean the scope (used by <c>return</c>/<c>goto</c>).
        /// </summary>
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

        /// <summary>
        /// Emits a <c>Clean</c> instruction that closes to-be-closed upvalues past the specified threshold.
        /// </summary>
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

        /// <summary>
        /// Emits the closure op for the given upvalue list and jump target (function body).
        /// </summary>
        /// <param name="symbols">Captured symbols.</param>
        /// <param name="jmpnum">Index to the function body.</param>
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

        /// <summary>
        /// Emits the Args instruction to describe captured symbols or explicit arguments.
        /// </summary>
        public Instruction EmitArgs(params SymbolRef[] symbols)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Args, SymbolList = symbols }
            );
        }

        /// <summary>
        /// Emits a return opcode with the specified number of values.
        /// </summary>
        public Instruction EmitRet(int retvals)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Ret, NumVal = retvals }
            );
        }

        /// <summary>
        /// Emits a number conversion helper used by <c>tonumber</c> and numeric for loops.
        /// </summary>
        /// <param name="stage">Stage of the conversion pipeline.</param>
        public Instruction EmitToNum(int stage = 0)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.ToNum, NumVal = stage }
            );
        }

        /// <summary>
        /// Emits the increment opcode used by numeric for loops.
        /// </summary>
        /// <param name="i">Step amount.</param>
        public Instruction EmitIncr(int i)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Incr, NumVal = i }
            );
        }

        /// <summary>
        /// Emits a new table allocation instruction.
        /// </summary>
        /// <param name="shared">Whether the table is shared with nested closures.</param>
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

        /// <summary>
        /// Emits the iteration preparation opcode for generic for loops.
        /// </summary>
        public Instruction EmitIterPrep()
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.IterPrep }
            );
        }

        /// <summary>
        /// Emits an expression tuple expansion so multi-return functions can be aligned.
        /// </summary>
        /// <param name="stackOffset">Offset from the current stack top.</param>
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

        /// <summary>
        /// Emits the iterator update opcode used within generic for loops.
        /// </summary>
        public Instruction EmitIterUpd()
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.IterUpd }
            );
        }

        /// <summary>
        /// Emits a metadata instruction describing Lua-friendly names for runtime helpers.
        /// </summary>
        /// <param name="funcName">Name of the helper.</param>
        /// <param name="metaType">Metadata category.</param>
        /// <param name="value">Optional payload.</param>
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

        /// <summary>
        /// Emits the BeginFn opcode, supplying the debug symbols captured for the compiled function.
        /// </summary>
        /// <param name="stackFrame">Frame describing locals and to-be-closed vars.</param>
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

        /// <summary>
        /// Emits a scalar marker indicating the stack holds exactly one value (normalizing tuples).
        /// </summary>
        public Instruction EmitScalar()
        {
            return AppendInstruction(new Instruction(_currentSourceRef) { OpCode = OpCode.Scalar });
        }

        /// <summary>
        /// Emits the instructions necessary to load the specified symbol onto the stack.
        /// </summary>
        /// <param name="sym">Symbol to read.</param>
        /// <returns>Number of instructions emitted.</returns>
        /// <exception cref="InternalErrorException">Thrown when the symbol type is unknown.</exception>
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
                case SymbolRefType.UpValue:
                    AppendInstruction(
                        new Instruction(_currentSourceRef) { OpCode = OpCode.UpValue, Symbol = sym }
                    );
                    return 1;
                default:
                    throw new InternalErrorException("Unexpected symbol type : {0}", sym);
            }
        }

        /// <summary>
        /// Emits the instructions required to store a tuple value into the specified symbol.
        /// </summary>
        /// <param name="sym">Symbol to write.</param>
        /// <param name="stackofs">Stack offset where the tuple starts.</param>
        /// <param name="tupleidx">Index inside the tuple.</param>
        /// <returns>Number of instructions emitted.</returns>
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
                case SymbolRefType.UpValue:
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

        /// <summary>
        /// Emits the table-initializer opcode for next-field appends.
        /// </summary>
        public Instruction EmitTblInitN()
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.TblInitN }
            );
        }

        /// <summary>
        /// Emits the table-initializer opcode for array-style writes.
        /// </summary>
        /// <param name="lastpos">True when this is the final array fill, enabling fast path cleanup.</param>
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

        /// <summary>
        /// Emits the appropriate index instruction depending on the access style (name / expr list / regular).
        /// </summary>
        /// <param name="index">Optional literal index.</param>
        /// <param name="isNameIndex">Whether the index is a string literal.</param>
        /// <param name="isExpList">Whether the index was produced by an expression list.</param>
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

        /// <summary>
        /// Emits the appropriate index set instruction for the access style.
        /// </summary>
        /// <param name="stackofs">Stack offset containing the table.</param>
        /// <param name="tupleidx">Index within the tuple source.</param>
        /// <param name="index">Optional literal index.</param>
        /// <param name="isNameIndex">Whether the index is a string literal.</param>
        /// <param name="isExpList">Whether the index was produced by an expression list.</param>
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

        /// <summary>
        /// Emits a copy instruction that duplicates the top n stack slots (used by vararg propagation).
        /// </summary>
        /// <param name="numval">Number of values to copy.</param>
        public Instruction EmitCopy(int numval)
        {
            return AppendInstruction(
                new Instruction(_currentSourceRef) { OpCode = OpCode.Copy, NumVal = numval }
            );
        }

        /// <summary>
        /// Emits a swap instruction that exchanges two stack slots.
        /// </summary>
        /// <param name="p1">First stack index.</param>
        /// <param name="p2">Second stack index.</param>
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
